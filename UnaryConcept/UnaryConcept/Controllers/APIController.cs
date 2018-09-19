using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UnaryConcept.Core;
using UnaryConcept.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace UnaryConcept.Controllers
{    
    [Route("api/")]
    public class APIController : Controller
    {
        private IHostingEnvironment _environment;

        public APIController(IHostingEnvironment environment)
        {
            _environment = environment;
        }

        // GET: api/<controller>
        [HttpGet("parsing")]
        public APIModel GetQuery(String searchQuery, String physicalPath, bool IncludeMachineSuggestions)
        {
            APIModel aPIModel = new APIModel();
            String errorMsg = String.Empty;
            GeneralFunctions generalFunctions = new GeneralFunctions();

            if (string.IsNullOrWhiteSpace(searchQuery) && string.IsNullOrWhiteSpace(physicalPath))
            {
                string msg = "The Search Query and Physical path are required or missing";
                aPIModel.ErrorMessage = msg;
                generalFunctions.ErrorLogMessageToFile(msg, "GetQuery", "APIController", searchQuery, string.Empty, physicalPath, _environment);
            }
            else if (string.IsNullOrWhiteSpace(searchQuery))
            {
                string msg = "The Search Query is required or missing";
                aPIModel.ErrorMessage = msg;
                generalFunctions.ErrorLogMessageToFile(msg, "GetQuery", "APIController", searchQuery, string.Empty, physicalPath, _environment);
            }
            else if (string.IsNullOrWhiteSpace(physicalPath))
            {
                string msg = "The Physical path is required or missing";
                aPIModel.ErrorMessage = msg;
                generalFunctions.ErrorLogMessageToFile(msg, "GetQuery", "APIController", searchQuery, string.Empty, physicalPath, _environment);
            }
            else if (!generalFunctions.ValidateBalancedParentheses(searchQuery))
            {
                string msg = "Parenthesis is not balanced";
                aPIModel.ErrorMessage = msg;
                generalFunctions.ErrorLogMessageToFile(msg, "GetQuery", "APIController", searchQuery, string.Empty, physicalPath, _environment);
            }
            else if (!generalFunctions.ValidateBalancedCurlyBraces(searchQuery))
            {
                string msg = "Curly braces are not balanced";
                aPIModel.ErrorMessage = msg;
                generalFunctions.ErrorLogMessageToFile(msg, "GetQuery", "APIController", searchQuery, string.Empty, physicalPath, _environment);
            }
            else
            {
                string fileNameUploaded = string.Empty;
                if (physicalPath.Contains(@"\"))
                    fileNameUploaded = physicalPath.Substring(physicalPath.LastIndexOf('\\') + 1);
                else
                    fileNameUploaded = physicalPath;

                String newFileNameWithSynonyms = string.Empty;

                if (IncludeMachineSuggestions)
                {
                    //perform the validation here too for concept library 
                    //create synonym words

                    generalFunctions.ActivityLogMessageToFile("CreateSynonymWords (Method Begining)", "APIController", searchQuery, fileNameUploaded, physicalPath, _environment, string.Empty);

                    Task<String> tempFileNameWithSynonyms = generalFunctions.CreateSynonymWords(null, fileNameUploaded, _environment, searchQuery);
                    newFileNameWithSynonyms = tempFileNameWithSynonyms.Result;

                    generalFunctions.ActivityLogMessageToFile("CreateSynonymWords (Method End)", "APIController", searchQuery, fileNameUploaded, physicalPath, _environment, "Output from CreateSynonymWords method : FileNameWithSynonyms = " + newFileNameWithSynonyms);
                }

                Task<Tuple<string, IFileInfo, string>> UploadFileResult;

                if (!string.IsNullOrWhiteSpace(newFileNameWithSynonyms))
                {
                    generalFunctions.ActivityLogMessageToFile("UploadFile (Method Begining)", "APIController", searchQuery, newFileNameWithSynonyms, physicalPath, _environment, string.Empty);

                    //Uploading .csv file into wwwroot path.
                    UploadFileResult = generalFunctions.UploadFile(null, newFileNameWithSynonyms, _environment, true, searchQuery);

                    generalFunctions.ActivityLogMessageToFile("UploadFile (Method End)", "APIController", searchQuery, newFileNameWithSynonyms, physicalPath, _environment, "Output from UploadFile method : FileName = " + UploadFileResult.Result.Item1 + "; FilePhysicalPath = " + UploadFileResult.Result.Item2.PhysicalPath);
                }
                else
                {
                    generalFunctions.ActivityLogMessageToFile("UploadFile (Method Begining)", "APIController", searchQuery, fileNameUploaded, physicalPath, _environment, string.Empty);

                    //Uploading .csv file into wwwroot path.
                    UploadFileResult = generalFunctions.UploadFile(null, fileNameUploaded, _environment, true, searchQuery);

                    generalFunctions.ActivityLogMessageToFile("UploadFile (Method End)", "APIController", searchQuery, physicalPath, String.Empty, _environment, "Output from UploadFile method : FileName --" + UploadFileResult.Result.Item1 + "; FilePhysicalPath : " + UploadFileResult.Result.Item2.PhysicalPath);
                    
                }

                if (!string.IsNullOrWhiteSpace(UploadFileResult.Result.Item3))
                {
                    aPIModel.ErrorMessage = UploadFileResult.Result.Item3;
                }
                else
                {
                    string fileName = UploadFileResult.Result.Item1;
                    IFileInfo fileInfo = UploadFileResult.Result.Item2;

                    if (String.IsNullOrEmpty(fileName))
                    {
                        string msg = "Invalid File Path";
                        aPIModel.ErrorMessage = msg;
                        generalFunctions.ErrorLogMessageToFile(msg, "GetQuery", "APIController", searchQuery, fileNameUploaded, physicalPath, _environment);
                        return aPIModel;
                    }

                    QueryParserTemp qp = new QueryParserTemp();
                    ConceptViewModel cvm = new ConceptViewModel();

                    generalFunctions.ActivityLogMessageToFile("ParseQuery (Method Begining)", "APIController", searchQuery, physicalPath, string.Empty, _environment, string.Empty);

                    //Parsing the Search query and creating Google and Bing queries.
                    ConceptViewModel result = qp.ParseQuery(searchQuery, fileInfo.PhysicalPath, out aPIModel, out errorMsg, cvm, _environment);

                    generalFunctions.ActivityLogMessageToFile("ParseQuery (Method End)", "APIController", searchQuery, physicalPath, string.Empty, _environment, "Output from ParseQuery method : UploadedFileName = " + result.UploadedFileName +
                        "; UploadedFileNameFromDropDownList = " + result.UploadedFileNameAndDropDown + "; Translation = " + result.Translation + "; GoogleQuery = " + result.GoogleQuery + "; BingQuery = " + result.BingQuery);
                    
                    if (!string.IsNullOrWhiteSpace(errorMsg))
                        aPIModel.ErrorMessage = errorMsg;
                }
            }
            return aPIModel;
        }
    }
}
