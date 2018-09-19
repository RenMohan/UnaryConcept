using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnaryConcept.Model
{
    public class APIModel
    {
        public String ErrorMessage { get; set; }
        public String GoogleQuery { get; set; }
        public String BingQuery { get; set; }
        public String Translation { get; set; }        
    }
}
