using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnaryConcept.Model
{
    public class ConceptViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        public String SearchQuery { get; set; }

        public IFormFile LibPath { get; set; }

        public String GoogleQuery { get; set; }
        public String BingQuery { get; set; }
        public String Translation { get; set; }

        public String UploadedFileName { get; set; }

        public bool isSynonymChecked { get; set; }

        public String resetClicked { get; set; }

        [NotMapped]
        public List<string> UploadedFileNameAndDropDown { get; set; }
    }
}
