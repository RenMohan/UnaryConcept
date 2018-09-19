using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnaryConcept.Model;

namespace UnaryConcept.Core
{
    public class QueryParserTemp
    {
        private const char curlyBracesOpen = '{';
        private const char curlyBracesClose = '}';
        private const char csvfilecolumnseparator = ',';
        private const String queryBeginEnclosure = "(";
        private const String queryEndEnclosure = ")";
        private const String disjunctionOperator = " or ";

        public ConceptViewModel ParseQuery(String query, String libPath, out APIModel aPIModel, out String errorMessage, ConceptViewModel cvm, IHostingEnvironment environment)
        {
            GeneralFunctions generalFunctions = new GeneralFunctions();
            //StreamReader reader = new StreamReader(libPath);
            errorMessage = String.Empty;
            aPIModel = new APIModel();
            try
            {
                Boolean hasMoreCurlyBraces = false;
                String queryTemp = query;
                List<String> notPresentInFileList = new List<string>();

                //first validate the query
                GeneralFunctions gf = new GeneralFunctions();

                do
                {
                    String queryExpansion = string.Empty;
                    if (queryTemp.Contains(curlyBracesOpen))
                    {

                        string regularExpressionPattern = @"\{(.*?)\}";

                        Regex re = new Regex(regularExpressionPattern);
                        MatchCollection matches = re.Matches(queryTemp);

                        List<String> matchesList = new List<String>();


                        foreach (Match m in re.Matches(queryTemp))
                        {
                            if (!notPresentInFileList.Contains(m.Value))
                            {
                                matchesList.Add(m.Value);
                            }
                        }

                        if (!matchesList.Any())
                            hasMoreCurlyBraces = false;

                        foreach (var match in matchesList)
                        {
                            Console.WriteLine(match);

                            List<String> filecontent = new List<String>();
                            char[] charArray = new char[] { curlyBracesOpen, curlyBracesClose };

                            var textInCurlyBraces = match.Trim(charArray);

                            if (!String.IsNullOrEmpty(libPath))
                            {
                                try
                                {
                                    int count = 0;
                                    using (StreamReader reader = File.OpenText(libPath))
                                    {
                                        string line = string.Empty;
                                        while ((line = reader.ReadLine()) != null)
                                        {
                                            line = line.Replace("\"\"", "\"");
                                            string[] strlist = new string[] { };
                                            strlist = line.Split(',');
                                            line = string.Empty;

                                            if (strlist[1].StartsWith("\"") && strlist[1].EndsWith("\""))
                                                line = strlist[0] + ',' + strlist[1].Substring(1, strlist[1].Length - 2);
                                            else
                                                line = strlist[0] + ',' + strlist[1];

                                            count++;
                                            if (gf.ValidateBalancedParentheses(line))
                                            {
                                                if (gf.ValidateBalancedCurlyBraces(line))
                                                {
                                                    String parentConcept = line.Substring(0, line.IndexOf(","));

                                                    if (parentConcept.Contains("{") || parentConcept.Contains("}"))
                                                        errorMessage = "Column 1 cannot contain curly braces in the concept library in line " + count;

                                                    if ((line.ToLowerInvariant().StartsWith(textInCurlyBraces.ToLowerInvariant() + csvfilecolumnseparator)))
                                                    {
                                                        filecontent.Add(line);
                                                    }
                                                }
                                                else
                                                    errorMessage = "Curly braces are not balanced in concept bank at line " + count;
                                            }
                                            else
                                                errorMessage = "Parenthesis are not balanced in concept bank at line " + count;
                                        }
                                        reader.Close();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //errorMessage = ex.Message; 
                                    generalFunctions.ErrorLogMessageToFile(ex.Message, "ParseQuery", "QueryParserTemp", query, cvm.UploadedFileName, libPath, environment);
                                }
                            }

                            for (int j = 0; j < filecontent.Count(); j++)
                            {
                                String parsestring = filecontent.ToArray()[j].ToString();
                                int csvfilecolumnseparator_begin_loc = parsestring.IndexOf(csvfilecolumnseparator);

                                if (String.IsNullOrEmpty(queryExpansion))
                                {
                                    queryExpansion = parsestring.Substring(csvfilecolumnseparator_begin_loc + 1);

                                    queryExpansion = queryBeginEnclosure + queryExpansion + queryEndEnclosure;
                                }
                                else
                                    queryExpansion = queryExpansion + disjunctionOperator + parsestring.Substring(csvfilecolumnseparator_begin_loc + 1);
                            }
                            if (!String.IsNullOrEmpty(queryExpansion))
                                queryExpansion = queryBeginEnclosure + queryExpansion + queryEndEnclosure;
                            else
                            {
                                queryExpansion = match;
                                if (!notPresentInFileList.Contains(match))
                                    notPresentInFileList.Add(match);
                            }

                            queryTemp = queryTemp.Replace(match, queryExpansion);
                            queryExpansion = "";
                        }
                    }
                }

                while (hasMoreCurlyBraces);

                String finalquery = queryTemp;

                String[] finalqueryArray = finalquery.Split(" ");

                String googleQuery = "https://www.google.com/search?q=";
                String bingQuery = "https://www.bing.com/search?q=";

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < finalqueryArray.Length; i++)
                {
                    googleQuery = googleQuery + finalqueryArray[i] + "+";
                    bingQuery = bingQuery + finalqueryArray[i] + "+";
                }
                String googleUrl;
                String bingUrl;

                if (googleQuery != null)
                {
                    googleUrl = googleQuery.Substring(0, googleQuery.Length - 1);
                }
                else
                    googleUrl = "";

                if (bingQuery != null)
                {
                    bingUrl = bingQuery.Substring(0, bingQuery.Length - 1);
                }
                else
                    bingUrl = "";

                cvm.GoogleQuery = googleUrl;
                cvm.BingQuery = bingUrl;
                cvm.SearchQuery = query;
                cvm.Translation = finalquery;

                aPIModel.GoogleQuery = googleUrl;
                aPIModel.BingQuery = bingUrl;
                aPIModel.Translation = finalquery;

                Console.WriteLine("Google Query: " + googleUrl);
                Console.WriteLine("Bing Query: " + bingUrl);

                StringBuilder sbuilder = new StringBuilder();

                sbuilder.AppendLine("Search Query: " + query);
                sbuilder.Append(Environment.NewLine);
                sbuilder.AppendLine("Translation: " + finalquery);
                sbuilder.Append(Environment.NewLine);
                sbuilder.AppendLine("Google URL: " + googleUrl);
                sbuilder.Append(Environment.NewLine);
                sbuilder.AppendLine("Bing URL: " + bingUrl);

                var result = sbuilder.ToString();
            }
            catch (Exception ex)
            {
                generalFunctions.ErrorLogMessageToFile(ex.Message, "ParseQuery", "QueryParserTemp", query, cvm.UploadedFileName, libPath, environment);
            }
            return cvm;
        }
    }
    
}
