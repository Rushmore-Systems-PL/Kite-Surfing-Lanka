using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class KiteTrainingRatesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _ktrUserId = -1;
        public static int _ktrRecId = -1;
        public static bool _ktrStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _ktrRecId = -1;
                _ktrStatus = false;
                int numTrainingLocationID = db.Locations.Where(l => l.varLocationName == "Kite School").OrderBy(x => x.varLocationName).Select(l => l.numLocationID).FirstOrDefault();
                var Items = (from it in db.Items
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where it.bitActive == true && it.bitIsDeleted == false  && lit.bitActive == true && lit.numLocationID == numTrainingLocationID
                             select new
                             {
                                 ITid = it.numItemID,
                                 ITdesc = it.varItemName,
                                 ITorder = it.dtCreatedDate
                             }).OrderBy(x => x.ITorder).ToList();
                ViewBag.numItemID = new SelectList(Items, "ITid", "ITdesc");
                ViewBag.KiteTrainingRates = db.KiteTrainingRates.Where(i => i.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KiteTrainingRate kiteTrainingRate)
        {
            kiteTrainingRate.bitActive = true;
            kiteTrainingRate.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            kiteTrainingRate.dtCreatedDate = System.DateTime.Now;

            db.KiteTrainingRates.Add(kiteTrainingRate);
            db.SaveChanges();

            TempData["KiteTrainingRateStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KiteTrainingRate kiteTrainingRate = db.KiteTrainingRates.Find(id);
            if (kiteTrainingRate == null)
            {
                return HttpNotFound();
            }
            _ktrRecId = kiteTrainingRate.numKiteTrainingRateID;
            _ktrStatus = true;
            _ktrUserId = Convert.ToInt32(Session["UserID"]);

            int numTrainingLocationID = db.Locations.Where(l => l.varLocationName == "Kite School").OrderBy(x => x.varLocationName).Select(l => l.numLocationID).FirstOrDefault();
            var Items = (from it in db.Items
                         join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                         where it.bitActive == true && it.bitIsDeleted == false  && lit.bitActive == true && lit.numLocationID == numTrainingLocationID
                         select new
                         {
                             ITid = it.numItemID,
                             ITdesc = it.varItemName,
                             ITorder = it.dtCreatedDate
                         }).OrderBy(x => x.ITorder).ToList();
            ViewBag.numItemID = new SelectList(Items, "ITid", "ITdesc", kiteTrainingRate.numItemID);
            return PartialView("_KiteTrainingRate", db.KiteTrainingRates.Where(c => c.numKiteTrainingRateID == id).FirstOrDefault<KiteTrainingRate>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KiteTrainingRate kiteTrainingRate)
        {
            kiteTrainingRate.bitActive = true;
            kiteTrainingRate.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            kiteTrainingRate.dtUpdatetDate = System.DateTime.Now;

            db.Entry(kiteTrainingRate).State = EntityState.Modified;
            db.SaveChanges();

            _ktrRecId = -1;
            _ktrStatus = false;

            TempData["KiteTrainingRateStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KiteTrainingRate kiteTrainingRate = db.KiteTrainingRates.Find(id);
            if (kiteTrainingRate == null)
            {
                return HttpNotFound();
            }

            kiteTrainingRate.bitActive = false;
            kiteTrainingRate.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            kiteTrainingRate.dtDeletedDate = System.DateTime.Now;

            db.Entry(kiteTrainingRate).State = EntityState.Modified;
            db.SaveChanges();

            TempData["KiteTrainingRateStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(decimal numKiteTrainingRateHours, int numItemID)
        {
            return Json(IsAvailable(numKiteTrainingRateHours, numItemID));
        }

        public bool IsAvailable(decimal numKiteTrainingRateHours, int numItemID)
        {
            bool status = false;
            if (_ktrStatus == false)
            {
                var KiteTrainingRate = (from E in db.KiteTrainingRates where E.numKiteTrainingRateHours == numKiteTrainingRateHours && E.numItemID == numItemID && E.bitActive == true select new { numKiteTrainingRateHours }).FirstOrDefault();
                if (KiteTrainingRate != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_ktrUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var KiteTrainingRate = (from E in db.KiteTrainingRates where E.numKiteTrainingRateID != _ktrRecId && E.numKiteTrainingRateHours == numKiteTrainingRateHours && E.numItemID == numItemID && E.bitActive == true select new { numKiteTrainingRateHours }).FirstOrDefault();
                    if (KiteTrainingRate != null)
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
            if (_ktrUserId == Convert.ToInt32(Session["UserID"]) && _ktrRecId == id)
            {
                _ktrRecId = -1;
                _ktrStatus = false;
                _ktrUserId = -1;
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