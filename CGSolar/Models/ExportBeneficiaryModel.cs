using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CGSolar.Models
{
    public class ExportBeneficiaryModel
    {
        public string WorkOrderNo { get; set; }
        public string PumpType { get; set; }
        public string systemCapacity { get; set; }
        public string BeneficiaryName { get; set; }
        public string FatherName { get; set; }
        public string ContactNo { get; set; }
        public string Village { get; set; }
        public string Block { get; set; }
        public string District { get; set; }
        public string Aadhar { get; set; }
        public string Caste { get; set; }
        public string ProjectType { get; set; }
    }
}