using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class PaymentTypesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _ptyUserId = -1;
        public static int _ptyRecId = -1;
        public static bool _ptyStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _ptyRecId = -1;
                _ptyStatus = false;
                ViewBag.PaymentTypes = db.PaymentTypes.Where(i => i.bitActive == true).OrderBy(x => x.varPaymentTypeName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PaymentType paymentType)
        {
            paymentType.bitActive = true;
            paymentType.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            paymentType.dtCreatedDate = System.DateTime.Now;

            db.PaymentTypes.Add(paymentType);
            db.SaveChanges();

            TempData["PaymentTypeStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PaymentType paymentType = db.PaymentTypes.Find(id);
            if (paymentType == null)
            {
                return HttpNotFound();
            }
            _ptyRecId = paymentType.numPaymentTypeID;
            _ptyStatus = true;
            _ptyUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_PaymentType", db.PaymentTypes.Where(c => c.numPaymentTypeID == id).FirstOrDefault<PaymentType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PaymentType paymentType)
        {
            paymentType.bitActive = true;
            paymentType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            paymentType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(paymentType).State = EntityState.Modified;
            db.SaveChanges();

            _ptyRecId = -1;
            _ptyStatus = false;

            TempData["PaymentTypeStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PaymentType paymentType = db.PaymentTypes.Find(id);
            if (paymentType == null)
            {
                return HttpNotFound();
            }

            paymentType.bitActive = false;
            paymentType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            paymentType.dtDeletedDate = System.DateTime.Now;

            db.Entry(paymentType).State = EntityState.Modified;
            db.SaveChanges();

            TempData["PaymentTypeStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varPaymentTypeName)
        {
            return Json(IsAvailable(varPaymentTypeName));
        }

        public bool IsAvailable(string varPaymentTypeName)
        {
            bool status = false;
            if (_ptyStatus == false)
            {
                var PaymentType = (from E in db.PaymentTypes where E.varPaymentTypeName.ToUpper() == varPaymentTypeName.ToUpper() && E.bitActive == true select new { varPaymentTypeName }).FirstOrDefault();
                if (PaymentType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_ptyUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var PaymentType = (from E in db.PaymentTypes where E.numPaymentTypeID != _ptyRecId && E.varPaymentTypeName.ToUpper() == varPaymentTypeName.ToUpper() && E.bitActive == true select new { varPaymentTypeName }).FirstOrDefault();
                    if (PaymentType != null)
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
            if (_ptyUserId == Convert.ToInt32(Session["UserID"]) && _ptyRecId == id)
            {
                _ptyRecId = -1;
                _ptyStatus = false;
                _ptyUserId = -1;
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