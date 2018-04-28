using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CGSolar.Models
{
    public class BeneficiaryDetailsModel
    {
        public string Category { get; set; }
        public List<tbl_beneficiary> BeneficiaryList { get; set; }
    }
}