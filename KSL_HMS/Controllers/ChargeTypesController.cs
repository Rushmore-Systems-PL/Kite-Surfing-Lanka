using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class ChargeTypesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _cgtUserId = -1;
        public static int _cgtRecId = -1;
        public static bool _cgtStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _cgtRecId = -1;
                _cgtStatus = false;
                ViewBag.ChargeTypes = db.ChargeTypes.Where(i => i.bitActive == true).OrderBy(x => x.varChargeTypeName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ChargeType chargeType)
        {
            chargeType.bitActive = true;
            chargeType.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            chargeType.dtCreatedDate = System.DateTime.Now;

            db.ChargeTypes.Add(chargeType);
            db.SaveChanges();

            TempData["ChargeTypeStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChargeType chargeType = db.ChargeTypes.Find(id);
            if (chargeType == null)
            {
                return HttpNotFound();
            }
            _cgtRecId = chargeType.numChargeTypeID;
            _cgtStatus = true;
            _cgtUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_ChargeType", db.ChargeTypes.Where(c => c.numChargeTypeID == id).FirstOrDefault<ChargeType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ChargeType chargeType)
        {
            chargeType.bitActive = true;
            chargeType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            chargeType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(chargeType).State = EntityState.Modified;
            db.SaveChanges();

            _cgtRecId = -1;
            _cgtStatus = false;

            TempData["ChargeTypeStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ChargeType chargeType = db.ChargeTypes.Find(id);
            if (chargeType == null)
            {
                return HttpNotFound();
            }

            chargeType.bitActive = false;
            chargeType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            chargeType.dtDeletedDate = System.DateTime.Now;

            db.Entry(chargeType).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ChargeTypeStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varChargeTypeName)
        {
            return Json(IsAvailable(varChargeTypeName));
        }

        public bool IsAvailable(string varChargeTypeName)
        {
            bool status = false;
            if (_cgtStatus == false)
            {
                var ChargeType = (from E in db.ChargeTypes where E.varChargeTypeName.ToUpper() == varChargeTypeName.ToUpper() && E.bitActive == true select new { varChargeTypeName }).FirstOrDefault();
                if (ChargeType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_cgtUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var ChargeType = (from E in db.ChargeTypes where E.numChargeTypeID != _cgtRecId && E.varChargeTypeName.ToUpper() == varChargeTypeName.ToUpper() && E.bitActive == true select new { varChargeTypeName }).FirstOrDefault();
                    if (ChargeType != null)
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
            if (_cgtUserId == Convert.ToInt32(Session["UserID"]) && _cgtRecId == id)
            {
                _cgtRecId = -1;
                _cgtStatus = false;
                _cgtUserId = -1;
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