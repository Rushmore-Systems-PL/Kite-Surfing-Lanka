using KSL_HMS.DB;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class GuestChargeTypesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _gcgtUserId = -1;
        public static int _gcgtRecId = -1;
        public static bool _gcgtStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _gcgtRecId = -1;
                _gcgtStatus = false;
                ViewBag.GuestChargeTypes = db.GuestChargeTypes.Where(i => i.bitActive == true).OrderBy(x => x.varGuestChargeTypeName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(GuestChargeType guestChargeType)
        {
            guestChargeType.bitActive = true;
            guestChargeType.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            guestChargeType.dtCreatedDate = System.DateTime.Now;

            db.GuestChargeTypes.Add(guestChargeType);
            db.SaveChanges();

            TempData["GuestChargeTypeStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GuestChargeType guestChargeType = db.GuestChargeTypes.Find(id);
            if (guestChargeType == null)
            {
                return HttpNotFound();
            }
            _gcgtRecId = guestChargeType.numGuestChargeTypeID;
            _gcgtStatus = true;
            _gcgtUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_GuestChargeType", db.GuestChargeTypes.Where(c => c.numGuestChargeTypeID == id).FirstOrDefault<GuestChargeType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(GuestChargeType guestChargeType)
        {
            guestChargeType.bitActive = true;
            guestChargeType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            guestChargeType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(guestChargeType).State = EntityState.Modified;
            db.SaveChanges();

            _gcgtRecId = -1;
            _gcgtStatus = false;

            TempData["GuestChargeTypeStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GuestChargeType guestChargeType = db.GuestChargeTypes.Find(id);
            if (guestChargeType == null)
            {
                return HttpNotFound();
            }

            guestChargeType.bitActive = false;
            guestChargeType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            guestChargeType.dtDeletedDate = System.DateTime.Now;

            db.Entry(guestChargeType).State = EntityState.Modified;
            db.SaveChanges();

            TempData["GuestChargeTypeStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varGuestChargeTypeName)
        {
            return Json(IsAvailable(varGuestChargeTypeName));
        }

        public bool IsAvailable(string varGuestChargeTypeName)
        {
            bool status = false;
            if (_gcgtStatus == false)
            {
                var GuestChargeType = (from E in db.GuestChargeTypes where E.varGuestChargeTypeName.ToUpper() == varGuestChargeTypeName.ToUpper() && E.bitActive == true select new { varGuestChargeTypeName }).FirstOrDefault();
                if (GuestChargeType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_gcgtUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var GuestChargeType = (from E in db.GuestChargeTypes where E.numGuestChargeTypeID != _gcgtRecId && E.varGuestChargeTypeName.ToUpper() == varGuestChargeTypeName.ToUpper() && E.bitActive == true select new { varGuestChargeTypeName }).FirstOrDefault();
                    if (GuestChargeType != null)
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
            if (_gcgtUserId == Convert.ToInt32(Session["UserID"]) && _gcgtRecId == id)
            {
                _gcgtRecId = -1;
                _gcgtStatus = false;
                _gcgtUserId = -1;
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