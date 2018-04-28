using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CGSolar.Models;

namespace CGSolar.Controllers
{
    public class HomeController : Controller
    {
        GuptaAgroDBContext db = new GuptaAgroDBContext();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult OandMSheet()
        {
            var list = db.tbl_OandM.Select(o => o).Distinct().ToList();
            List<OperationAndMaintenanceModel> oNmList = new List<OperationAndMaintenanceModel>();
            foreach(var maintenanceDetails in list)
            {
                OperationAndMaintenanceModel oNmObj = new OperationAndMaintenanceModel();
                oNmObj.BeneficiaryName = maintenanceDetails.tbl_beneficiary.BeneficiaryName;
                oNmObj.Block = maintenanceDetails.tbl_beneficiary.Block;
                oNmObj.CompletionDate = maintenanceDetails.DateOfCompletion;
                oNmObj.Contact = maintenanceDetails.tbl_beneficiary.ContactNo;
                oNmObj.District = maintenanceDetails.tbl_beneficiary.District;
                oNmObj.ProblemDescription = maintenanceDetails.ProblemType;
                oNmObj.ReportedDate = maintenanceDetails.ProblemreportedOn;
                oNmObj.SystemCapacity = maintenanceDetails.tbl_beneficiary.systemCapacity;
                oNmObj.Status = maintenanceDetails.Status;
                oNmObj.Village = maintenanceDetails.tbl_beneficiary.Village;
                oNmObj.WorkOrderNo = maintenanceDetails.WorkOrderID;
                oNmList.Add(oNmObj);
            }
            
            return View(oNmList);
        }

        public ActionResult ComplaintForm()
        {
            ViewBag.EmployeeList = db.tbl_employee.Select(e => e).ToList();
            ViewBag.PumpTypeList = db.tbl_beneficiary.Select(e => e.PumpType).Distinct().ToList();
            return View();
        }

        [HttpPost]
        public ActionResult ComplaintForm(FormCollection form)
        {
            string workOrder = form["WorkOrder"].ToString();
            int? beneficiaryID = Convert.ToInt32(form["benID"]);
            int assignedTo = Convert.ToInt32(form["AssignedTo"]);
            string employeeName = db.tbl_employee.Where(e => e.EmployeeID == assignedTo).Select(e => e.EmployeeName).FirstOrDefault();
            string aadhar = db.tbl_beneficiary.Where(b => b.BeneficiaryID == beneficiaryID).Select(b => b.Aadhar).FirstOrDefault();
            DateTime reportedDate = Convert.ToDateTime(form["reportedDate"]).AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute).AddSeconds(DateTime.Now.Second).AddMilliseconds(DateTime.Now.Millisecond);
            string problemType = form["ProblemDescription"].ToString();

            db.usp_complaint(workOrder,beneficiaryID,employeeName,aadhar,reportedDate,problemType);
            return RedirectToAction("OandMSheet", "Home");
        }
        public ActionResult BeneficiaryDetails()
        {
            var categoryList = db.tbl_beneficiary.OrderBy(b => b.category).Select(b => b.category).ToList().Distinct();
            ViewBag.category = categoryList;

            return View();
        }
        [HttpPost]
        public ActionResult BeneficiaryDetails(BeneficiaryDetailsModel model)
        {
            var categoryList = db.tbl_beneficiary.OrderBy(b => b.category).Select(b => b.category).ToList().Distinct();
            ViewBag.category = categoryList;

            var beneficiaryList = db.tbl_beneficiary.Where(b => b.category == model.Category).Select(b => b).ToList();

            BeneficiaryDetailsModel benDetails = new BeneficiaryDetailsModel();
            benDetails.BeneficiaryList = beneficiaryList;
            return View(benDetails);
        }

        [HttpPost]
        public JsonResult AutoComplete(string prefix)
        {                        
            var list = db.tbl_beneficiary.Where(b => b.BeneficiaryName.ToLower().Contains(prefix.ToLower())).Select(b => new { label = b.BeneficiaryName, value = b.BeneficiaryID }).Distinct();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AutoFill(int id)
        {

            var list = db.tbl_beneficiary.Where(b => b.BeneficiaryID == id).Select(b => new { name = b.BeneficiaryName, workOrder = b.WorkOrderNo, village = b.Village, block = b.Block, district = b.District, contact = b.ContactNo, systemCapacity = b.systemCapacity, pumpType = b.PumpType }).FirstOrDefault();
            return Json(list, JsonRequestBehavior.AllowGet);
        }
    }
}