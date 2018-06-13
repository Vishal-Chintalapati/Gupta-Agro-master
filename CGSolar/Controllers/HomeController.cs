using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using CGSolar.Models;
using System.Reflection;
using System.Xml.Serialization;
using ClosedXML;
using ClosedXML.Excel;
using System.IO;
using System.Configuration;
using System.Data.OleDb;

namespace CGSolar.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        GuptaAgroDbContext db = new GuptaAgroDbContext();
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
        [Authorize(Roles = "Admin")]
        public ActionResult Register()
        {
            ViewBag.Roles = db.tbl_roles.Select(r => r).ToList();
            return View();
        }
        [HttpPost]
        public ActionResult Register(FormCollection form)
        {
            ViewBag.Roles = db.tbl_roles.Select(r => r).ToList();
            string empName = form["empName"].ToString();
            string contact = form["contact"].ToString();
            string pwd = form["password"].ToString();
            int role = Convert.ToInt32(form["role"]);
            var roleDesc = db.tbl_roles.Where(r => r.roleid == role).Select(r => r.role).FirstOrDefault();
            var uid = db.usp_generateUserID(empName, contact, roleDesc);

            var userid = uid.First();
            db.usp_RegisterUser(empName,userid,roleDesc,contact,pwd,DateTime.Now,Environment.UserName);
            var empList = db.tbl_employee.Select(e => e).ToList();
            return View(empList);
        }
        [HttpPost]
        public JsonResult UserCheck(string empName,string contact, int role)
        {
            var roleDesc = db.tbl_roles.Where(r => r.roleid == role).Select(r => r.role).FirstOrDefault();
            var uid = db.usp_generateUserID(empName,contact,roleDesc);
            
            var userid = uid.First();
            return Json(userid,JsonRequestBehavior.AllowGet);
        }

        //public ActionResult Login()
        //{
        //    return View();
        //}
        //public JsonResult LoginValidation(string UserName, string Password)
        //{
        //    return Json(!db.tbl_employee.Any(emp=>(emp.userid == UserName && emp.password == Password)||(emp.ContactNo == UserName && emp.password == Password)),JsonRequestBehavior.AllowGet);
        //}
        //[HttpPost]
        //public ActionResult Login(LoginModel login)
        //{
        //    try
        //    {
        //        var role = db.tbl_employee.Where(e => e.userid == login.UserName && e.password == login.Password).Select(e => e.Role).FirstOrDefault();
        //        Session["role"] = role;
        //        Session.Timeout = 30;
        //        if (role == "Admin")
        //        {
        //            return RedirectToAction("AdminDashboard");
        //        }
        //        else if (role == "Manager")
        //        {
        //            return RedirectToAction("ManagerDashboard");
        //        }
        //        else if (role == "Field Assitant")
        //        {
        //            return RedirectToAction("FADashboard");
        //        }
        //        else
        //        {
        //            throw new Exception();
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        return View("Error");
        //    }
        //}

        [Authorize(Roles ="Field Assitant,Admin,Manager")]
        public ActionResult OandMSheet()
        {
            ViewBag.EmployeeList = db.tbl_employee.Where(e => e.Role == "Field Assitant").Select(e => e).ToList();
            ViewBag.ItemList = db.tbl_Items.Select(i => i).ToList();
            ViewBag.PumpTypeList = db.tbl_beneficiary.Select(b => b.PumpType).Distinct().ToList();
            ViewBag.Status = new List<string>() { "Submitted", "PARTIALLY COMPLETED", "PENDING" };
            var list = db.tbl_OandM.Select(o => o).Distinct().ToList();
            if (Session["role"].ToString() == "Field Assitant")
            {
                list = list.Where(o => o.AssignedTo == Session["ID"].ToString()).Select(o => o).ToList();
            }
            else if (Session["role"].ToString() == "Manager")
            {
                list = list.Where(o => o.Created_By == Session["ID"].ToString()).Select(o => o).ToList();
            }
            else if(Session["role"].ToString() == "Admin")
            {
                list = list;
            }
            List<OperationAndMaintenanceModel> oNmList = new List<OperationAndMaintenanceModel>();
            foreach(var maintenanceDetails in list)
            {
                int assignedTo =  Convert.ToInt32(maintenanceDetails.AssignedTo);
                OperationAndMaintenanceModel oNmObj = new OperationAndMaintenanceModel();
                oNmObj.BeneficiaryName = maintenanceDetails.tbl_beneficiary.BeneficiaryName;
                oNmObj.BeneficiaryID = maintenanceDetails.tbl_beneficiary.BeneficiaryID;
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
                oNmObj.AssignedTo = assignedTo;/*db.tbl_employee.Where(e=>e.EmployeeName.Trim() == assignedTo.Trim()).Select(e=>e.EmployeeID).FirstOrDefault() ;*/
                oNmObj.AssignedEmployee = db.tbl_employee.Where(e => e.EmployeeID == assignedTo).Select(e => e.EmployeeName).FirstOrDefault();
                oNmList.Add(oNmObj);
            }
            
            return View(oNmList);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Field Assitant")]
        public JsonResult EditOandM(int benID,string benName, string workOrder, string village, string block, string district,string sysCapacity,string contact, string reportDate, string completionDate, string problemDesc, string status, int assignedTo)
        {
            var benObj = db.tbl_beneficiary.Where(b => b.BeneficiaryID == benID).Select(b => b).FirstOrDefault();
            
            benObj.BeneficiaryName = benName;
            benObj.WorkOrderNo = workOrder;
            benObj.Village = village;
            benObj.Block = block;
            benObj.District = district;
            benObj.systemCapacity = sysCapacity;
            benObj.ContactNo = contact;

            var OandMObj = db.tbl_OandM.Where(o => o.BeneficiaryID == benID).Select(o => o).FirstOrDefault();

            OandMObj.DateOfCompletion = Convert.ToDateTime(completionDate);
            OandMObj.ProblemreportedOn = Convert.ToDateTime(reportDate);
            OandMObj.ProblemType = problemDesc;
            OandMObj.Status = status;
            OandMObj.WorkOrderID = workOrder;
            string assignedEmp = db.tbl_employee.Where(e => e.EmployeeID == assignedTo).Select(e => e.EmployeeName).FirstOrDefault();
            OandMObj.AssignedTo = assignedTo.ToString();

            db.SaveChanges();
            return Json("Success", JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "Manager")]
        public ActionResult ComplaintForm()
        {
            ViewBag.EmployeeList = db.tbl_employee.Where(e=>e.Role == "Field Assitant").Select(e => e).ToList();
            ViewBag.PumpTypeList = db.tbl_beneficiary.Select(e => e.PumpType).Distinct().ToList();
            return View();
        }

        [HttpPost]
        public ActionResult ComplaintForm(FormCollection form)
        {
            string workOrder = form["WorkOrder"].ToString();
            int? beneficiaryID = Convert.ToInt32(form["benID"]);
            string assignedTo = form["AssignedTo"].ToString();
            //string employeeName = db.tbl_employee.Where(e => e.EmployeeID == assignedTo).Select(e => e.EmployeeName).FirstOrDefault();
            string aadhar = db.tbl_beneficiary.Where(b => b.BeneficiaryID == beneficiaryID).Select(b => b.Aadhar).FirstOrDefault();
            DateTime reportedDate = Convert.ToDateTime(form["reportedDate"]).AddHours(DateTime.Now.Hour).AddMinutes(DateTime.Now.Minute).AddSeconds(DateTime.Now.Second).AddMilliseconds(DateTime.Now.Millisecond);
            string problemType = form["ProblemDescription"].ToString();
            string createdBy = Session["ID"].ToString();

            db.usp_complaint(workOrder,beneficiaryID,assignedTo,aadhar,reportedDate,problemType,createdBy);
            return RedirectToAction("OandMSheet", "Home");
        }

        public JsonResult FieldForm(string problemDesc, int itemCode, int qty, string qtyType, string pumpType,string workOrder)
        {
            string data = "";
            int empID = Convert.ToInt32(Session["ID"]);
            var oNmObj = db.tbl_OandM.Where(o => o.WorkOrderID == workOrder).Select(o => o).FirstOrDefault();
            var distribitionItem = db.tbl_distributions.Where(d=>d.itemcode == itemCode && d.employeeID == empID).Select(p => p).FirstOrDefault();
            oNmObj.ActualProblem = problemDesc;
            oNmObj.tbl_beneficiary.PumpType = pumpType;
            if(qtyType == "inhand")
            {
                if(distribitionItem.Remainingquantity >= qty)
                {
                    distribitionItem.Remainingquantity -= qty;
                    distribitionItem.usedquantity += qty;
                    oNmObj.Status = "PARTIALLY COMPLETED";
                }
                else
                {
                    data = "qty";
                }
                
            }
            else if(qtyType == "required")
            {
                tbl_StockRequest stockReq = new tbl_StockRequest();
                stockReq.EmployeeID = empID;
                stockReq.EmployeeName = db.tbl_employee.Where(e => e.EmployeeID == empID).Select(e => e.EmployeeName).FirstOrDefault();
                stockReq.ItemCode = itemCode;
                stockReq.ItemName = db.tbl_Items.Where(i => i.itemcode == itemCode).Select(i => i.itemname).FirstOrDefault();
                stockReq.Status = "PENDING";
                oNmObj.Status = "PENDING";
                db.tbl_StockRequest.Add(stockReq);
            }
            db.SaveChanges();
            return Json(data,JsonRequestBehavior.AllowGet);
        } 

        [HttpPost]
        public JsonResult FetchRemQty(string itemCode)
        {
            var empID = Convert.ToInt32(Session["ID"]);
            int? remQty = 0;
            if (itemCode != "0")
            {
                var itmCode = Convert.ToInt32(itemCode);
                remQty = db.tbl_distributions.Where(d => d.employeeID == empID && d.itemcode == itmCode).Select(d => d.Remainingquantity).FirstOrDefault();
            }
            else
            {
                remQty = -1;
            }
            return Json(remQty,JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Field Assitant,Admin,Manager")]
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

        public static System.Data.DataTable ToDataTable<T>(List<T> items)
        {
            DataTable dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach(PropertyInfo prop in Props)
            {
                //Defining type of data column gives proper data table
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);

            }

            foreach(T item in items)
            {
                var values = new object[Props.Length];
                for(int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item,null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }

        public void ExportToExcelReports()
        {
            DataTable dt = new DataTable();
            dt.TableName = "tbl_BeneficiaryDetails";
            var beneficiaryDetails = db.tbl_beneficiary.Select(b => b).ToList();
            List<ExportBeneficiaryModel> downloadBeneficiaryList = new List<ExportBeneficiaryModel>();
            foreach(var item in beneficiaryDetails)
            {
                ExportBeneficiaryModel benDetails = new ExportBeneficiaryModel();
                benDetails.Aadhar = item.Aadhar;
                //benDetails.BeneficiaryID = item.BeneficiaryID;
                benDetails.BeneficiaryName = item.BeneficiaryName;
                benDetails.Block = item.Block;
                benDetails.Caste = item.Caste;
                benDetails.ProjectType = item.category;
                benDetails.ContactNo = item.ContactNo;
                benDetails.District = item.District;
                benDetails.FatherName = item.FatherName;
                benDetails.PumpType = item.PumpType;
                benDetails.systemCapacity = item.systemCapacity;
                benDetails.Village = item.Village;
                benDetails.WorkOrderNo = item.WorkOrderNo;
                downloadBeneficiaryList.Add(benDetails);
            }

            DataTable dataTable = ToDataTable(downloadBeneficiaryList);
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dataTable);
                wb.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wb.Style.Font.Bold = true;

                Response.Clear();
                Response.Buffer = true;
                Response.Charset = "";
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;filename= BeneficiaryList.xlsx");

                using (MemoryStream MyMemoryStream = new MemoryStream())
                {
                    wb.SaveAs(MyMemoryStream);
                    MyMemoryStream.WriteTo(Response.OutputStream);
                    Response.Flush();
                    Response.End();

                }
            }
        }


        public ActionResult UploadBeneficiary()
        {
            return View();
        }
        [HttpPost]
        public ActionResult UploadBeneficiary(HttpPostedFileBase file)
        {
            string filePath = string.Empty;
            string path = Server.MapPath("~/Uploads/~");
            filePath = path + Path.GetFileName(file.FileName);
            try
            {
                DataTable beneficiaryTable = new DataTable();
                if (Request.Files["file"].ContentLength > 0)
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string extension = Path.GetExtension(file.FileName);
                    file.SaveAs(filePath);

                    string conString = string.Empty;

                    switch (extension)
                    {
                        case ".xls":  //Excel 97-03.
                            conString = ConfigurationManager.ConnectionStrings["Excel03ConString"].ConnectionString;
                            break;
                        case ".xlsx":  //Excel 07 and above.
                            conString = ConfigurationManager.ConnectionStrings["Excel07ConString"].ConnectionString;
                            break;
                    }
                    DataTable dt = new DataTable();
                    conString = string.Format(conString, filePath);

                    //using (OleDbConnection connExcel = new OleDbConnection(conString))
                    //{
                    //    using (OleDbCommand cmdExcel = new OleDbCommand())
                    //    {
                    //        using (OleDbDataAdapter odaExcel = new OleDbDataAdapter())
                    //        {
                    //            cmdExcel.Connection = connExcel;

                    //            connExcel.Open();
                    //            DataTable dtExcelSchema;
                    //            dtExcelSchema = connExcel.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                    //            string sheetName = dtExcelSchema.Rows[0]["TABLE_NAME"].ToString();
                    //            connExcel.Close();

                    //            //Reading Data from First sheet
                    //            connExcel.Open();
                    //            cmdExcel.CommandText = "SELECT * FROM ["+sheetName+"]";
                    //            odaExcel.SelectCommand = cmdExcel;
                    //            odaExcel.Fill(dt);                                
                    //            connExcel.Close();
                    //        }
                    //    }
                    //}

                    OleDbConnection conn = new OleDbConnection(conString);
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand("SELECT * FROM [Sheet1$]", conn))
                    {
                        OleDbDataAdapter oleda = new OleDbDataAdapter();
                        oleda.SelectCommand = cmd;
                        DataSet ds = new DataSet();
                        oleda.Fill(ds);
                        dt = ds.Tables[0];
                    }
                    conn.Close();

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            if (dt.Rows[i][0].ToString() != "")
                            {
                                tbl_beneficiary benDetails = new tbl_beneficiary();
                                benDetails.WorkOrderNo = dt.Rows[1][0].ToString();
                                benDetails.PumpType = dt.Rows[i][1].ToString();
                                benDetails.systemCapacity = dt.Rows[i][2].ToString();
                                benDetails.BeneficiaryName = dt.Rows[i][3].ToString();
                                benDetails.FatherName = dt.Rows[i][4].ToString();
                                benDetails.ContactNo = dt.Rows[i][5].ToString();
                                benDetails.Village = dt.Rows[i][6].ToString();
                                benDetails.Block = dt.Rows[i][7].ToString();
                                benDetails.District = dt.Rows[i][8].ToString();
                                benDetails.Aadhar = dt.Rows[i][9].ToString();
                                benDetails.Caste = dt.Rows[i][10].ToString();
                                benDetails.category = dt.Rows[i][11].ToString();
                                db.tbl_beneficiary.Add(benDetails);
                                db.SaveChanges();
                            }
                            else
                            {
                                continue;
                            }
                        }

                    return RedirectToAction("BeneficiaryDetails","Home");
                }
            }
            catch (Exception ex)
            {
                return View("Error");
            }
            return View();
        }

        public ActionResult Inventory()
        {
            List<InventoryStock> stockList = new List<InventoryStock>();
            var inventoryStock = db.tbl_InventoryStock.Select(p => p).ToList();
            foreach(var stock in inventoryStock)
            {
                InventoryStock stockItem = new InventoryStock();
                stockItem.Amount = stock.Amount;
                stockItem.BilledDate = stock.BillDate;
                stockItem.BillNo = stock.BillNo;
                stockItem.SpareItem = stock.Item;
                stockItem.SpareQuantity = stock.quantity;
                stockItem.Tax = stock.Tax;
                stockItem.UnitCost = stock.Price;
                stockItem.VendorName = stock.VendorName;
                //stockItem.TotalAmount = (stock.Price* stock.quantity)+ stock.Tax
                stockList.Add(stockItem);

            }
            ViewBag.ItemList = db.tbl_Items.Select(p => p).ToList();
            return View(stockList);
        }

        [HttpPost]
        public JsonResult AddStock(string vendorName, string billNo, string billDate, string quantity, string amount, string tax, string itemName, string price, string spareItemText)
        {
            string data = "";
            int? itemCode = Convert.ToInt32(itemName);
            int empID = Convert.ToInt32(Session["ID"]);
            var items = db.tbl_Items.Select(p => p.itemname.ToLower().Trim()).ToList();
            if(itemCode == 0)
            {
                if (!items.Contains(spareItemText.ToLower().Trim()))
                {
                 
                    if (spareItemText != null || spareItemText != "")
                    {
                        tbl_Items item = new tbl_Items();
                        item.itemname = spareItemText;
                        item.createdby = db.tbl_employee.Where(e => e.EmployeeID == empID).Select(e => e.EmployeeName).FirstOrDefault();
                        item.createddate = DateTime.Now;
                        db.tbl_Items.Add(item);
                        db.SaveChanges();
                        data = "Item";
                    }                    
                    
                }
                else
                {
                    data = "Item exists";
                }
            }

            if(data == "Item exists")
            {
                data = data;
            }
            else
            {
                tbl_InventoryStock stock = new tbl_InventoryStock();
                stock.Amount = Convert.ToDecimal(amount);
                stock.BillDate = Convert.ToDateTime(billDate);
                stock.BillNo = billNo;
                stock.createdby = db.tbl_employee.Where(e => e.EmployeeID == empID).Select(e => e.EmployeeName).FirstOrDefault();
                stock.createddate = DateTime.Now;
                if (itemCode == 0)
                {
                    stock.Item = spareItemText;
                }
                else
                {
                    stock.Item = db.tbl_Items.Where(i => i.itemcode == itemCode).Select(i => i.itemname).FirstOrDefault();
                }
                stock.Price = Convert.ToDecimal(price);
                stock.quantity = Convert.ToInt32(quantity);
                stock.Tax = Convert.ToDecimal(tax);
                stock.VendorName = vendorName;
                db.tbl_InventoryStock.Add(stock);
                db.SaveChanges();
                data += "stock";
            }
            

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult StockDistribution()
        {
            ViewBag.EmployeeList = db.tbl_employee.Where(e => e.Role == "Field Assitant").Select(e => e).Distinct().ToList();
            ViewBag.ItemList = db.tbl_Items.Select(i => i).Distinct().ToList();
            var stockDistribution =  db.tbl_StockRequest.Select(s => s).ToList();
            return View(stockDistribution);
        }
        [HttpPost]
        public JsonResult DistributeStock(int employeeCode,int itemCode,int qty)
        {
            var empID = Convert.ToInt32(Session["ID"]);
            var stockDistributionObj = db.tbl_distributions.Where(d => d.employeeID == employeeCode && d.itemcode == itemCode).Select(d => d).FirstOrDefault();
            if(stockDistributionObj == null)
            {
                tbl_distributions stockDist = new tbl_distributions();
                stockDist.employeeID = employeeCode;
                stockDist.itemcode = itemCode;
                stockDist.Allocatedquantity = qty;
                stockDist.Remainingquantity = qty;
                stockDist.usedquantity = 0;
                stockDist.createdby = db.tbl_employee.Where(e => e.EmployeeID == empID).Select(e => e.EmployeeName).FirstOrDefault();
                stockDist.createdDate = DateTime.Now;
                db.tbl_distributions.Add(stockDist);                
            }
            else
            {
                stockDistributionObj.Remainingquantity += qty;
                stockDistributionObj.Allocatedquantity += qty;                                
            }
            db.SaveChanges();
            return Json("yes");
        }

        public ActionResult InventoryAdminONM()
        {
            List<string> status = new List<string>(){ "COMPLETED","PARTIALLY COMPLETED"};
            ViewBag.Status = status;
            List<OperationAndMaintenanceModel> oNmList = new List<OperationAndMaintenanceModel>();
            var invOnM = db.tbl_OandM.Where(o => o.Status == "PARTIALLY APPROVED").Select(o => o).Distinct().ToList();
            foreach(var item in invOnM)
            {
                OperationAndMaintenanceModel oNm = new OperationAndMaintenanceModel();
                var empId = Convert.ToInt32(item.AssignedTo);
                oNm.AssignedEmployee = db.tbl_employee.Where(e => e.EmployeeID == empId).Select(e => e.EmployeeName).FirstOrDefault();
                oNm.BeneficiaryName = item.tbl_beneficiary.BeneficiaryName;
                oNm.Block = item.tbl_beneficiary.Block;
                oNm.CompletionDate = item.DateOfCompletion;
                oNm.Contact = item.tbl_beneficiary.ContactNo;
                oNm.District = item.tbl_beneficiary.District;
                oNm.ProblemDescription = item.ProblemType;
                oNm.ReportedDate = item.ProblemreportedOn;
                oNm.Status = item.Status;
                oNm.SystemCapacity = item.tbl_beneficiary.systemCapacity;
                oNm.Village = item.tbl_beneficiary.Village;
                oNm.WorkOrderNo = item.WorkOrderID;
                oNmList.Add(oNm);
            }
            return View(oNmList);
        }

        [HttpPost]
        public ActionResult InventoryAdminONM(List<OperationAndMaintenanceModel> model )
        {
            foreach(var item in model)
            {
                if (item.Status != null)
                {
                    var oNmObj = db.tbl_OandM.Where(o => o.WorkOrderID == item.WorkOrderNo).Select(o => o).FirstOrDefault();
                    oNmObj.Status = "COMPLETED";
                    db.SaveChanges();
                }
                else
                {
                    continue;
                }
            }            
            return View();
        }
    }
}