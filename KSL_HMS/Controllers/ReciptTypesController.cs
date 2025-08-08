using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class ReciptTypesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _rptUserId = -1;
        public static int _rptRecId = -1;
        public static bool _rptStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _rptRecId = -1;
                _rptStatus = false;
                ViewBag.ReciptTypes = db.ReciptTypes.Where(i => i.bitActive == true).OrderBy(x => x.varReciptTypeName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ReciptType reciptType)
        {
            reciptType.bitActive = true;
            reciptType.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            reciptType.dtCreatedDate = System.DateTime.Now;

            db.ReciptTypes.Add(reciptType);
            db.SaveChanges();

            TempData["ReciptTypeStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ReciptType reciptType = db.ReciptTypes.Find(id);
            if (reciptType == null)
            {
                return HttpNotFound();
            }
            _rptRecId = reciptType.numReciptTypeID;
            _rptStatus = true;
            _rptUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_ReciptType", db.ReciptTypes.Where(c => c.numReciptTypeID == id).FirstOrDefault<ReciptType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ReciptType reciptType)
        {
            reciptType.bitActive = true;
            reciptType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            reciptType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(reciptType).State = EntityState.Modified;
            db.SaveChanges();

            _rptRecId = -1;
            _rptStatus = false;

            TempData["ReciptTypeStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ReciptType reciptType = db.ReciptTypes.Find(id);
            if (reciptType == null)
            {
                return HttpNotFound();
            }

            reciptType.bitActive = false;
            reciptType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            reciptType.dtDeletedDate = System.DateTime.Now;

            db.Entry(reciptType).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ReciptTypeStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varReciptTypeName)
        {
            return Json(IsAvailable(varReciptTypeName));
        }

        public bool IsAvailable(string varReciptTypeName)
        {
            bool status = false;
            if (_rptStatus == false)
            {
                var ReciptType = (from E in db.ReciptTypes where E.varReciptTypeName.ToUpper() == varReciptTypeName.ToUpper() && E.bitActive == true select new { varReciptTypeName }).FirstOrDefault();
                if (ReciptType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_rptUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var ReciptType = (from E in db.ReciptTypes where E.numReciptTypeID != _rptRecId && E.varReciptTypeName.ToUpper() == varReciptTypeName.ToUpper() && E.bitActive == true select new { varReciptTypeName }).FirstOrDefault();
                    if (ReciptType != null)
                    { status = false; }
                    else
                    { status = true; }
                    return status;
                }
                else
                {
                    return status;
                }
            }
        }

        public ActionResult ChangeValidationStatus(int? id)
        {
            if (_rptUserId == Convert.ToInt32(Session["UserID"]) && _rptRecId == id)
            {
                _rptRecId = -1;
                _rptStatus = false;
                _rptUserId = -1;
            }
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}