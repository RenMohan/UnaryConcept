using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UnaryConcept.Model;
using UnaryConcept.Core;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.Collections;
using WordNet;
using System.Runtime.InteropServices;
using System.Text;

namespace UnaryConcept.Core
{
    public class GeneralFunctions
    {
        private WnLexicon.Lexicon wordNET = null;
        public Wnlib.SearchSet bobj2;
        public Wnlib.SearchSet bobj3;
        public Wnlib.SearchSet bobj4;
        public Wnlib.SearchSet bobj5;

        public async Task<Tuple<string, IFileInfo, string>> UploadFile(IFormFile LibPath, string fileNameUploaded, IHostingEnvironment environment, Boolean restAPI, string searchQuery)
        {
            var fileName = string.Empty;
            string errorMsg = string.Empty;
            IFileInfo fileInfo = null;
            string fileNamePhysicalPathForLog = string.Empty;

            try
            {
                if (LibPath != null || (LibPath == null && !string.IsNullOrWhiteSpace(fileNameUploaded)))
                {
                    long size = 0;
                    IFormFile file = null;
                    if (LibPath != null)
                    {
                        file = LibPath;
                        fileName = Path.GetFileName(file.FileName);
                        size += file.Length;
                    }
                    else if (LibPath == null && !string.IsNullOrWhiteSpace(fileNameUploaded))
                        fileName = fileNameUploaded;

                    var pathToGetInfo = "wwwroot/" + fileName;
                    var filePath = environment.WebRootPath + $@"/{fileName}";
                    if (filePath != null)
                        fileNamePhysicalPathForLog = filePath;

                    if (!restAPI)
                    {
                        if (!System.IO.File.Exists(filePath))
                        {
                            using (FileStream fs = System.IO.File.Create(filePath))
                            {
                                await file.CopyToAsync(fs);
                                await fs.FlushAsync();
                            }
                        }
                        else
                        {
                            if (LibPath != null)
                            {
                                var newResult = string.Empty;
                                using (var reader = new StreamReader(file.OpenReadStream()))
                                {
                                    newResult = reader.ReadToEnd();
                                    reader.Close();
                                }

                                var existingResult = System.IO.File.ReadAllText(filePath);
                                if (!existingResult.Equals(newResult))
                                {
                                    System.IO.File.Delete(filePath);
                                    using (FileStream fs = System.IO.File.Create(filePath))
                                    {
                                        await file.CopyToAsync(fs);
                                        await fs.FlushAsync();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if(FileExists(filePath, searchQuery, fileName, environment) == false)
                        {
                            errorMsg = "File does not exist.Please upload the file through UI";
                        }
                    }

                    IFileProvider provider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
                    IDirectoryContents contents = provider.GetDirectoryContents(""); // the applicationRoot contents
                    fileInfo = provider.GetFileInfo(pathToGetInfo);
                }
            }
            catch (Exception ex)
            {
                ErrorLogMessageToFile(ex.Message, "UploadFile", "GeneralFunctions", searchQuery, fileName, fileNamePhysicalPathForLog, environment);
            }
            return new Tuple<string, IFileInfo, string>(fileName, fileInfo, errorMsg);
        }

        public static bool FileExists(string filePath, string searchQuery, string fileName, IHostingEnvironment environment)
        {
            string pathCheck = Path.GetDirectoryName(filePath);

            string filePart = Path.GetFileName(filePath);

            if (string.IsNullOrEmpty(pathCheck))
            {
                GeneralFunctions generalFunctions = new GeneralFunctions();
                string errorMsg = "The file must include a full path";
                generalFunctions.ErrorLogMessageToFile(errorMsg, "FileExists", "GeneralFunctions", searchQuery, fileName, filePath, environment);      
            }

            string[] checkFiles = Directory.GetFiles(pathCheck, filePart, SearchOption.TopDirectoryOnly);

            if (checkFiles != null && checkFiles.Length > 0)
            {
                //must be a binary compare

                return Path.GetFileName(checkFiles[0].ToLowerInvariant()) == filePart.ToLowerInvariant();
            }
            return false;
        }

        public Boolean ValidateBalancedParentheses(String conceptQuery)
        {
            return (from aChar in conceptQuery.ToCharArray()
                    where aChar == '('
                    select aChar).Count()
                    ==
                   (from aChar in conceptQuery.ToCharArray()
                    where aChar == ')'
                    select aChar).Count();
        }

        public Boolean ValidateBalancedCurlyBraces(String conceptQuery)
        {
            return (from aChar in conceptQuery.ToCharArray()
                    where aChar == '{'
                    select aChar).Count()
                    ==
                   (from aChar in conceptQuery.ToCharArray()
                    where aChar == '}'
                    select aChar).Count();
        }

        public Task<String> CreateSynonymWords(IFormFile libPath, string fileNameUploaded, IHostingEnvironment environment, string searchQuery)
        {
            string resultFileName = string.Empty;
            Random random = new Random();
            var fileName = string.Empty;
            var newFileName = string.Empty;

            if (libPath != null)
                fileName = Path.GetFileName(libPath.FileName);
            else if (libPath == null && !string.IsNullOrWhiteSpace(fileNameUploaded))
                fileName = fileNameUploaded;

            if (fileName.Contains(".csv"))
                newFileName = fileName.Substring(0, fileName.IndexOf('.')) + " - " + random.Next() + ".csv";
            else
                newFileName = fileName + " - " + random.Next() + ".csv";

            var filePath = environment.WebRootPath + $@"/{newFileName}";
            var dropDownFilePath = environment.WebRootPath + $@"/{fileNameUploaded}";
            var dictPath = environment.ContentRootPath;

            if (libPath != null)
            {
                try
                {
                    using (StreamReader pathreader = new StreamReader(libPath.OpenReadStream()))
                    {
                        AppendingSynonyms(pathreader, libPath, filePath, dropDownFilePath, dictPath, searchQuery, environment);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogMessageToFile(ex.Message, "CreateSynonymWords", "GeneralFunctions", searchQuery, fileName, filePath, environment);
                }
            }
            else if (System.IO.File.Exists(dropDownFilePath))
            {
                try
                {
                    using (StreamReader pathreader = new StreamReader(dropDownFilePath))
                    {
                        AppendingSynonyms(pathreader, libPath, filePath, dropDownFilePath, dictPath, searchQuery, environment);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogMessageToFile(ex.Message, "CreateSynonymWords", "GeneralFunctions", searchQuery, fileName, filePath, environment);
                }
            }

            if (System.IO.File.Exists(filePath))
                resultFileName = newFileName;

            return Task.FromResult(resultFileName);
        }

        public void AppendingSynonyms(StreamReader pathreader, IFormFile libPath, String newFilePath, String dropDownFilePath, String dictPath, string searchQuery, IHostingEnvironment environment)
        {
            List<String> words = new List<string>();
            String contents = String.Empty;
            var line = string.Empty;

            try
            {
                while ((line = pathreader.ReadLine()) != null)
                {
                    String parentConcept = line.Substring(0, line.IndexOf(","));
                    List<String> tempWords = new List<String>();

                    if (!words.Contains(parentConcept))
                    {
                        tempWords.Add(parentConcept);
                        words.Add(parentConcept);

                        var result = GetSynonyms(tempWords, dictPath);

                        if (result != null && result.Any())
                        {
                            if (!System.IO.File.Exists(newFilePath))
                            {
                                if (libPath == null)
                                {
                                    string content = System.IO.File.ReadAllText(dropDownFilePath);
                                    System.IO.File.AppendAllText(newFilePath, content);
                                    string newFilePathcontent = System.IO.File.ReadAllText(newFilePath);
                                }
                                else
                                {
                                    using (FileStream fs = System.IO.File.Create(newFilePath))
                                    {
                                        if (libPath != null)
                                        {
                                            libPath.CopyToAsync(fs);
                                            fs.FlushAsync();
                                            fs.Close();
                                        }
                                    }
                                }
                            }

                            if (libPath != null)
                            {
                                using (StreamReader newReader = new StreamReader(libPath.OpenReadStream()))
                                {
                                    contents = newReader.ReadToEnd();
                                    newReader.Close();
                                }
                            }
                            else
                            {
                                using (StreamReader newReader = new StreamReader(dropDownFilePath))
                                {
                                    contents = newReader.ReadToEnd();
                                    newReader.Close();
                                }
                            }

                            foreach (var item in result.ToList())
                            {
                                StringBuilder sb = new StringBuilder();
                                var newLine = item.BaseWord + "," + item.SynmWord;

                                if (!contents.Contains(newLine.ToString()))
                                {
                                    sb.Append(newLine + Environment.NewLine);
                                    System.IO.File.AppendAllText(newFilePath, sb.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogMessageToFile(ex.Message, "AppendingSynonyms", "GeneralFunctions", searchQuery, libPath.Name, libPath.FileName, environment);
            }
            finally
            {
                pathreader.Close();
            }
        }

        public IEnumerable<SynonymModel> GetSynonyms(IEnumerable<string> words, String dictPath)
        {
            IEnumerable<SynonymModel> synonyms = Enumerable.Empty<SynonymModel>();
            IEnumerable<SynonymModel> synonyms2 = Enumerable.Empty<SynonymModel>();

            synonyms2 = GetWordnetSynonyms(words, dictPath);

            if (!synonyms.Any() && synonyms2 != null && synonyms2.Any())
                synonyms = synonyms.Union(synonyms2);
            else if (synonyms.Any() && synonyms2 != null && synonyms2.Any())
                synonyms = MergeSynonyms(synonyms, synonyms2);

            return synonyms;
        }

        private IEnumerable<SynonymModel> RetainSynonymsForProvidedPOS(IEnumerable<SynonymModel> synonyms, IEnumerable<string> partOfSpeech)
        {
            List<SynonymModel> finalSynonyms = new List<SynonymModel>();

            if (synonyms == null || partOfSpeech == null || (partOfSpeech != null && !partOfSpeech.Any())) return finalSynonyms;

            foreach (var synm in synonyms)
            {
                if (synm == null || (synm != null && (string.IsNullOrWhiteSpace(synm.SynmWord) || string.IsNullOrWhiteSpace(synm.Type)))) continue;

                if (partOfSpeech.Any(x => synm.Type.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
                    finalSynonyms.Add(synm);
            }

            return finalSynonyms;
        }

        private IEnumerable<SynonymModel> MergeSynonyms(IEnumerable<SynonymModel> synonymsMain, IEnumerable<SynonymModel> synonyms2)
        {
            List<SynonymModel> synonyms = new List<SynonymModel>(1000);
            if (synonymsMain != null && synonyms2 != null)
            {
                synonyms = new List<SynonymModel>(synonymsMain);

                foreach (var synm in synonyms2)
                {
                    if (synonymsMain.Any(x => x.SynmWord.Equals(synm.SynmWord, StringComparison.InvariantCultureIgnoreCase))) continue;

                    synonyms.Add(synm);
                }
            }

            return synonyms;
        }

        private IEnumerable<SynonymModel> GetWordnetSynonyms(IEnumerable<string> words, String dictPath)
        {
            List<SynonymModel> synonyms = new List<SynonymModel>(100);            

            foreach (var word in words)
            {
                var wnSynonyms = WordnetFindtheinfo(null, word, dictPath);

                if (wnSynonyms == null || !wnSynonyms.Any()) continue;

                foreach (var synm in wnSynonyms)
                {
                    var synonym = new SynonymModel();
                    synonym.BaseWord = word;
                    synonym.SynmWord = synm;

                    synonyms.Add(synonym);
                }
            }

            var synonymsFinal = RemoveSynonymsIdenticalToBaseWords(synonyms);

            return synonymsFinal;
        }

        private IEnumerable<SynonymModel> RemoveSynonymsIdenticalToBaseWords(IEnumerable<SynonymModel> synonyms)
        {
            if (synonyms == null) return null;

            List<SynonymModel> synonymsFinal = new List<SynonymModel>(1000);
            foreach (var synm in synonyms)
            {
                if (synm.BaseWord.Equals(synm.SynmWord, StringComparison.InvariantCultureIgnoreCase)) continue;
                synonymsFinal.Add(synm);
            }

            return synonymsFinal;
        }

        private HashSet<String> ParseSynonyms(String[] synonyms)
        {
            HashSet<String> synonymList = new HashSet<String>();

            if (synonyms.Any())
            {
                int nSense = 0;
                string[] tmpSynonyms = synonyms;
                for (int i = 0; i < tmpSynonyms.Length; i++)
                {
                    if (!tmpSynonyms[i].StartsWith("Sense") && tmpSynonyms[i].Contains("--"))
                    {
                        nSense++;
                        if (nSense <= 2)
                        {
                            string[] tmpArray = tmpSynonyms[i].Substring(0, tmpSynonyms[i].IndexOf("--")).Split(',');
                            for (int j = 0; j < tmpArray.Length; j++)
                            {
                                synonymList.Add(tmpArray[j].Trim());
                            }
                        }

                        if (nSense >= 2)
                            break;
                    }
                }
            }
            return synonymList;
        }

        private List<String> WordnetFindtheinfo(Hashtable serviceData, string word, String dictPath)
        {
            List<String> finalString = new List<String>();
            if (wordNET == null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Wnlib.PartsOfSpeech pos = Wnlib.PartsOfSpeech.Noun;
                    switch (i)
                    {
                        case 0:
                            pos = Wnlib.PartsOfSpeech.Noun;
                            break;

                        case 1:
                            pos = Wnlib.PartsOfSpeech.Verb;
                            break;

                        case 2:
                            pos = Wnlib.PartsOfSpeech.Adj;
                            break;

                        case 3:
                            pos = Wnlib.PartsOfSpeech.Adv;
                            break;
                    }
                    string[] retVal = new string[] { };

                    retVal = WnLexicon.Lexicon.FindSynonyms(word, pos, false);

                    WordNetClasses.WN wnc = new WordNetClasses.WN(dictPath);

                    bool flag = false;
                    ArrayList list = new ArrayList();
                    wnc.OverviewFor(word, "noun", ref flag, ref bobj2, list);

                    wnc.OverviewFor(word, "verb", ref flag, ref bobj3, list);

                    wnc.OverviewFor(word, "adj", ref flag, ref bobj4, list);

                    wnc.OverviewFor(word, "adv", ref flag, ref bobj5, list);

                    if (retVal != null)
                    {
                        foreach (var arrayItem in retVal)
                        {

                            if (!finalString.Contains(arrayItem))
                                finalString.Add(arrayItem);

                            if (finalString.Count == 20)
                                break;
                        }
                    }
                }               
            }
            return finalString;
        }

        public void ErrorLogMessageToFile(string errorMsg, string methodName, string className, string searchQueryForLog, string uploadedFileNameForLog, string fileNamePhysicalPathForLog, IHostingEnvironment environment)
        {
            string fileName = "ErrorLog.txt";
            var pathToGetInfo = "./" + fileName;

            System.IO.StreamWriter sw = System.IO.File.AppendText(
                pathToGetInfo);
            try
            {
                string logLine = System.String.Format(
                    "{0:G}", "DateAndTime : " + System.DateTime.Now + " | " + "ErrorMessage : " + errorMsg + " | " + "ClassName : " + className + 
                    "MethodName : " + methodName + " | " + "SearchQuery :" + searchQueryForLog + "UploadedFileName" + uploadedFileNameForLog + " | " 
                    + "FileNamePhysicalPath : " + fileNamePhysicalPathForLog + " | " + "EnvironmentName : " + environment.EnvironmentName + " | "
                    + "ApplicationName : " + environment.ApplicationName + " | " + "WebRootPath : " + environment.WebRootPath + " | " + "ContentRootPath : " + environment.ContentRootPath);

                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }

        public void ActivityLogMessageToFile(string methodName, string className, string SearchQuery, string UploadedFileName, string UploadedFileNamePath, IHostingEnvironment environment, string outputResult)
        {
            string fileName = "ActivityLog.txt";
            var pathToGetInfo = "./" + fileName;

            System.IO.StreamWriter sw = System.IO.File.AppendText(
                pathToGetInfo);
            try
            {
                string logLine = System.String.Format(
                    "{0:G}", "DateAndTime : " + System.DateTime.Now + " | " + "MethodName : " + methodName + " | " +
                    "ClassName : " + className + " | " + "EnvironmentName : " + environment.EnvironmentName + " | " + "ApplicationName : " + environment.ApplicationName +
                    " | " + "WebRootPath : " + environment.WebRootPath + " | " + "ContentRootPath : " + environment.ContentRootPath
                    + " | " + "Output : " + outputResult);

                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }
}
