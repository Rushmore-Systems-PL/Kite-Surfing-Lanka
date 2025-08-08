using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class KiteRentalRatesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _krrUserId = -1;
        public static int _krrRecId = -1;
        public static bool _krrStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _krrRecId = -1;
                _krrStatus = false;
                int numRentalLocationID = db.Locations.Where(l => l.varLocationName == "Rental").Select(l => l.numLocationID).FirstOrDefault();
                var Items = (from it in db.Items
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where it.bitActive == true && it.bitIsDeleted == false && lit.bitActive == true && lit.numLocationID == numRentalLocationID
                             select new
                             {
                                 ITid = it.numItemID,
                                 ITdesc = it.varItemName,
                                 ITorder = it.dtCreatedDate
                             }).OrderBy(x => x.ITorder).ToList();
                ViewBag.numItemID = new SelectList(Items, "ITid", "ITdesc");
                ViewBag.KiteRentalRates = db.KiteRentalRates.Where(i => i.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KiteRentalRate kiteRentalRate)
        {
            kiteRentalRate.bitActive = true;
            kiteRentalRate.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            kiteRentalRate.dtCreatedDate = System.DateTime.Now;

            db.KiteRentalRates.Add(kiteRentalRate);
            db.SaveChanges();

            TempData["KiteRentalRateStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KiteRentalRate kiteRentalRate = db.KiteRentalRates.Find(id);
            if (kiteRentalRate == null)
            {
                return HttpNotFound();
            }
            _krrRecId = kiteRentalRate.numKiteRentalRateID;
            _krrStatus = true;
            _krrUserId = Convert.ToInt32(Session["UserID"]);

            int numRentalLocationID = db.Locations.Where(l => l.varLocationName == "Rental").Select(l => l.numLocationID).FirstOrDefault();
            var Items = (from it in db.Items
                         join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                         where it.bitActive == true && it.bitIsDeleted == false  && lit.bitActive == true && lit.numLocationID == numRentalLocationID
                         select new
                         {
                             ITid = it.numItemID,
                             ITdesc = it.varItemName,
                             ITorder = it.dtCreatedDate
                         }).OrderBy(x => x.ITorder).ToList();
            ViewBag.numItemID = new SelectList(Items, "ITid", "ITdesc", kiteRentalRate.numItemID);
            return PartialView("_KiteRentalRate", db.KiteRentalRates.Where(c => c.numKiteRentalRateID == id).FirstOrDefault<KiteRentalRate>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KiteRentalRate kiteRentalRate)
        {
            kiteRentalRate.bitActive = true;
            kiteRentalRate.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            kiteRentalRate.dtUpdatetDate = System.DateTime.Now;

            db.Entry(kiteRentalRate).State = EntityState.Modified;
            db.SaveChanges();

            _krrRecId = -1;
            _krrStatus = false;

            TempData["KiteRentalRateStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KiteRentalRate kiteRentalRate = db.KiteRentalRates.Find(id);
            if (kiteRentalRate == null)
            {
                return HttpNotFound();
            }

            kiteRentalRate.bitActive = false;
            kiteRentalRate.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            kiteRentalRate.dtDeletedDate = System.DateTime.Now;

            db.Entry(kiteRentalRate).State = EntityState.Modified;
            db.SaveChanges();

            TempData["KiteRentalRateStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(decimal numKiteRentalRateDays, int numItemID)
        {
            return Json(IsAvailable(numKiteRentalRateDays, numItemID));
        }

        public bool IsAvailable(decimal numKiteRentalRateDays, int numItemID)
        {
            bool status = false;
            if (_krrStatus == false)
            {
                var KiteRentalRate = (from E in db.KiteRentalRates where E.numKiteRentalRateDays == numKiteRentalRateDays && E.numItemID == numItemID && E.bitActive == true select new { numKiteRentalRateDays }).FirstOrDefault();
                if (KiteRentalRate != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_krrUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var KiteRentalRate = (from E in db.KiteRentalRates where E.numKiteRentalRateID != _krrRecId && E.numKiteRentalRateDays == numKiteRentalRateDays && E.numItemID == numItemID && E.bitActive == true select new { numKiteRentalRateDays }).FirstOrDefault();
                    if (KiteRentalRate != null)
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
            if (_krrUserId == Convert.ToInt32(Session["UserID"]) && _krrRecId == id)
            {
                _krrRecId = -1;
                _krrStatus = false;
                _krrUserId = -1;
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