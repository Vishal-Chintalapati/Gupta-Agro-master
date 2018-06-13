using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CGSolar.Models
{
    public class InventoryStock
    {
        public string VendorName { get; set; }
        public string SpareItem { get; set; }
        public int? SpareQuantity { get; set; }
        public string BillNo { get; set; }
        public DateTime? BilledDate { get; set; }

        public Decimal? UnitCost { get; set; }
        public Decimal? Amount { get; set; }
        public Decimal? Tax { get; set; }
        public Decimal? TotalAmount { get; set; }
        
    }
}