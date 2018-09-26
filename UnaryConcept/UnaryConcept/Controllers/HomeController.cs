using Microsoft.AspNetCore.Mvc;
using UnaryConcept.Model;
using UnaryConcept.Core;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using System.Net.Http.Headers;
using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UnaryConcept.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _environment;
        private List<string> allCSVFilesList = new List<string>();
        ConceptViewModel resultCvm = new ConceptViewModel();
        string dropDownDefaultName = "Please select or upload a concept bank";

        public HomeController(IHostingEnvironment environment, ILogger<HomeController> logger)
        {
            _environment = environment;

            string startFolder = _environment.WebRootPath;
            DirectoryInfo dir = new DirectoryInfo(startFolder);                      
            List<FileInfo> fileNamesList = dir.GetFiles("*.csv", SearchOption.AllDirectories).ToList();
            resultCvm.UploadedFileNameAndDropDown = fileNamesList.Select(x => x.Name).ToList();
            allCSVFilesList = resultCvm.UploadedFileNameAndDropDown;
            allCSVFilesList.Insert(0, dropDownDefaultName);
        }

        public IActionResult Index()
        {               
            return View(resultCvm);
        }       

        [HttpPost]
        public IActionResult Index(ConceptViewModel vm, string fileNameUploaded)
        {
            string searchQueryForLog = string.Empty;
            string uploadedFileNameForLog = string.Empty;
            string fileNamePhysicalPathForLog = string.Empty;

            GeneralFunctions generalFunctions = new GeneralFunctions();

            if (vm.resetClicked == "Reset")
            {
                ConceptViewModel resultCvm = new ConceptViewModel();
                allCSVFilesList.Insert(0, dropDownDefaultName);
                ModelState.Clear();
            }
            else
            {
                generalFunctions.ActivityLogMessageToFile("Index (Method Begining)", "HomeController", vm.SearchQuery, string.Empty, string.Empty, _environment, string.Empty);

                if (!string.IsNullOrWhiteSpace(fileNameUploaded))
                {
                    fileNameUploaded = fileNameUploaded.Substring(fileNameUploaded.LastIndexOf('/') + 1);
                    uploadedFileNameForLog = fileNameUploaded;
                }

                String errorMsg = String.Empty;

                if (ModelState.IsValid)
                {                    
                    try
                    {                        
                        if (vm.LibPath == null && (vm.SearchQuery.Contains("{") || vm.SearchQuery.Contains("}")))
                        {
                            if (vm.LibPath == null && string.IsNullOrWhiteSpace(fileNameUploaded))
                            {
                                string msg = "*Please upload a valid Concept Bank";
                                ModelState.AddModelError("SearchQuery", msg);
                                generalFunctions.ErrorLogMessageToFile(msg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                                return View(resultCvm);
                            }
                        }

                        if (vm.SearchQuery != null)
                        {
                            searchQueryForLog = vm.SearchQuery;
                            if (generalFunctions.ValidateBalancedParentheses(vm.SearchQuery))
                            {
                                if (!generalFunctions.ValidateBalancedCurlyBraces(vm.SearchQuery))
                                {
                                    string msg = "Curly braces are not balanced";
                                    ModelState.AddModelError("SearchQuery", msg);
                                    generalFunctions.ErrorLogMessageToFile(msg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                                    return View(resultCvm);
                                }
                            }
                            else
                            {
                                string msg = "Parenthesis is not balanced";
                                ModelState.AddModelError("SearchQuery", msg);
                                generalFunctions.ErrorLogMessageToFile(msg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                                return View(resultCvm);
                            }
                        }

                        if (vm.LibPath != null || (vm.LibPath == null && !string.IsNullOrWhiteSpace(fileNameUploaded)) || vm.LibPath == null)
                        {
                            String newFileNameWithSynonyms = string.Empty;

                            if (vm.isSynonymChecked)
                            {
                                //perform the validation here too for concept library 
                                //create synonym words
                                generalFunctions.ActivityLogMessageToFile("CreateSynonymWords (Method Begining)", "HomeController", vm.SearchQuery, fileNameUploaded, string.Empty, _environment, string.Empty);

                                Task<String> tempFileNameWithSynonyms = generalFunctions.CreateSynonymWords(vm.LibPath, fileNameUploaded, _environment, vm.SearchQuery);
                                newFileNameWithSynonyms = tempFileNameWithSynonyms.Result;

                                generalFunctions.ActivityLogMessageToFile("CreateSynonymWords (Method End)", "HomeController", vm.SearchQuery, fileNameUploaded, string.Empty, _environment, "Output from CreateSynonymWords method : FileNameWithSynonyms = " + newFileNameWithSynonyms);
                            }

                            Task<Tuple<string, IFileInfo, string>> UploadFileResult;

                            if (!string.IsNullOrWhiteSpace(newFileNameWithSynonyms))
                            {
                                generalFunctions.ActivityLogMessageToFile("UploadFile (Method Begining)", "HomeController", vm.SearchQuery, newFileNameWithSynonyms, string.Empty, _environment, string.Empty);

                                UploadFileResult = generalFunctions.UploadFile(null, newFileNameWithSynonyms, _environment, false, vm.SearchQuery);

                                generalFunctions.ActivityLogMessageToFile("UploadFile (Method End)", "HomeController", vm.SearchQuery, newFileNameWithSynonyms, String.Empty, _environment, "Output from UploadFile method : FileName = " + UploadFileResult.Result.Item1 + "; FilePhysicalPath = " + UploadFileResult.Result.Item2.PhysicalPath);
                            }
                            else
                            {
                                generalFunctions.ActivityLogMessageToFile("UploadFile (Method Begining)", "HomeController", vm.SearchQuery, fileNameUploaded, fileNamePhysicalPathForLog, _environment, string.Empty);

                                //Uploading .csv file into wwwroot path.
                                UploadFileResult = generalFunctions.UploadFile(vm.LibPath, fileNameUploaded, _environment, false, vm.SearchQuery);

                                generalFunctions.ActivityLogMessageToFile("UploadFile (Method End)", "HomeController", vm.SearchQuery, fileNameUploaded, String.Empty, _environment, "Output from UploadFile method : FileName --" + UploadFileResult.Result.Item1 + "; FilePhysicalPath : " + UploadFileResult.Result.Item2.PhysicalPath);
                            }

                            string fileName = UploadFileResult.Result.Item1;
                            IFileInfo fileInfo = UploadFileResult.Result.Item2;
                            errorMsg = UploadFileResult.Result.Item3;

                            APIModel aPIModel = new APIModel();
                            QueryParserTemp qp = new QueryParserTemp();

                            if (fileInfo == null)
                            {
                                generalFunctions.ActivityLogMessageToFile("ParseQuery (Method Begining)", "HomeController", vm.SearchQuery, string.Empty, string.Empty, _environment, string.Empty);

                                //Parsing the Search query and creating Google and Bing queries.
                                resultCvm = qp.ParseQuery(vm.SearchQuery, null, out aPIModel, out errorMsg, resultCvm, _environment);

                                generalFunctions.ActivityLogMessageToFile("ParseQuery (Method End)", "HomeController", vm.SearchQuery, string.Empty, string.Empty, _environment, "Output from ParseQuery method : UploadedFileName = " + resultCvm.UploadedFileName +
                                    "; UploadedFileNameFromDropDownList = " + resultCvm.UploadedFileNameAndDropDown + "; Translation = " + resultCvm.Translation + "; GoogleQuery = " + resultCvm.GoogleQuery + "; BingQuery = " + resultCvm.BingQuery);
                            }
                            else
                            {
                                uploadedFileNameForLog = fileInfo.Name;
                                fileNamePhysicalPathForLog = fileInfo.PhysicalPath;

                                generalFunctions.ActivityLogMessageToFile("ParseQuery (Method Begining)", "HomeController", vm.SearchQuery, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment, string.Empty);

                                //Parsing the Search query and creating Google and Bing queries.
                                resultCvm = qp.ParseQuery(vm.SearchQuery, fileInfo.PhysicalPath, out aPIModel, out errorMsg, resultCvm, _environment);

                                generalFunctions.ActivityLogMessageToFile("ParseQuery (Method End)", "HomeController", vm.SearchQuery, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment, "Output from ParseQuery method : UploadedFileName = " + resultCvm.UploadedFileName +
                                    "; UploadedFileNameFromDropDownList = " + resultCvm.UploadedFileNameAndDropDown + "; Translation = " + resultCvm.Translation + "; GoogleQuery = " + resultCvm.GoogleQuery + "; BingQuery = " + resultCvm.BingQuery);
                            }

                            if ((String.IsNullOrEmpty(errorMsg) && vm.LibPath != null) || (String.IsNullOrEmpty(errorMsg) && vm.LibPath == null))
                            {
                                if (!string.IsNullOrWhiteSpace(fileName))
                                {
                                    int indx = allCSVFilesList.FindIndex(x => x == fileName);

                                    if (indx >= 0)
                                        allCSVFilesList.RemoveAt(indx);

                                    allCSVFilesList.Insert(0, fileName);
                                    resultCvm.UploadedFileNameAndDropDown = allCSVFilesList;

                                    resultCvm.UploadedFileName = fileName;
                                }
                                else
                                {
                                    resultCvm.UploadedFileName = allCSVFilesList[0];
                                    resultCvm.UploadedFileNameAndDropDown = allCSVFilesList;
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("SearchQuery", errorMsg);
                                generalFunctions.ErrorLogMessageToFile(errorMsg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                            }
                        }
                    }
                    catch (Exception ex)
                    {                       
                        generalFunctions.ErrorLogMessageToFile(ex.Message, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);                        
                    }
                }
                else
                {
                    foreach (var key in ModelState.Keys)
                    {
                        ModelState[key].Errors.Clear();

                        if (vm.SearchQuery == null && vm.LibPath == null)
                        {
                            string msg = "*The Search Query and Concept Bank are required";
                            ModelState.AddModelError("SearchQuery", msg);
                            generalFunctions.ErrorLogMessageToFile(msg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                        }
                        else if (vm.SearchQuery == null)
                        {
                            string msg = "*The Search Query is required";
                            ModelState.AddModelError("SearchQuery", msg);
                            generalFunctions.ErrorLogMessageToFile(msg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                        }
                        else if (vm.LibPath == null)
                        {
                            string msg = "*Please upload a valid Concept Bank";
                            ModelState.AddModelError("SearchQuery", msg);
                            generalFunctions.ErrorLogMessageToFile(msg, "Index", "HomeController", searchQueryForLog, uploadedFileNameForLog, fileNamePhysicalPathForLog, _environment);
                        }
                    }
                }
            }
            generalFunctions.ActivityLogMessageToFile("Index (Method End)", "HomeController", vm.SearchQuery, resultCvm.UploadedFileName, fileNamePhysicalPathForLog, _environment, "Return value from Index method : " + "UploadedFileNameFromDropDownList = " + resultCvm.UploadedFileNameAndDropDown + "; Translation = " + resultCvm.Translation + "; GoogleQuery = " + resultCvm.GoogleQuery + "; BingQuery = " + resultCvm.BingQuery);
            return View(resultCvm);
        }

        public IActionResult Options()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }
    }
}
