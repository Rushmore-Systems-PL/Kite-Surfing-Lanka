using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class LocationItemTypesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _loiUserId = -1;
        public static int _loiRecId = -1;
        public static bool _loiStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _loiRecId = -1;
                _loiStatus = false;
                ViewBag.numItemTypeID = new SelectList(db.ItemTypes.Where(i => i.bitActive == true).OrderBy(x => x.varItemTypeName), "numItemTypeID", "varItemTypeName");
                ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName");
                ViewBag.LocationItemTypes = db.LocationItemTypes.Where(i => i.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(LocationItemType locationItemType)
        {
            locationItemType.bitActive = true;
            locationItemType.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            locationItemType.dtCreatedDate = System.DateTime.Now;

            db.LocationItemTypes.Add(locationItemType);
            db.SaveChanges();

            TempData["LocationItemStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LocationItemType locationItemType = db.LocationItemTypes.Find(id);
            if (locationItemType == null)
            {
                return HttpNotFound();
            }
            _loiRecId = locationItemType.numLocationItemTypeID;
            _loiStatus = true;
            _loiUserId = Convert.ToInt32(Session["UserID"]);

            ViewBag.numItemTypeID = new SelectList(db.ItemTypes.Where(i => i.bitActive == true).OrderBy(x => x.varItemTypeName), "numItemTypeID", "varItemTypeName", locationItemType.numItemTypeID);
            ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName", locationItemType.numLocationID);
            return PartialView("_LocationItemType", db.LocationItemTypes.Where(c => c.numLocationItemTypeID == id).FirstOrDefault<LocationItemType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(LocationItemType locationItemType)
        {
            locationItemType.bitActive = true;
            locationItemType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            locationItemType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(locationItemType).State = EntityState.Modified;
            db.SaveChanges();

            _loiRecId = -1;
            _loiStatus = false;

            TempData["LocationItemStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LocationItemType locationItemType = db.LocationItemTypes.Find(id);
            if (locationItemType == null)
            {
                return HttpNotFound();
            }

            locationItemType.bitActive = false;
            locationItemType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            locationItemType.dtDeletedDate = System.DateTime.Now;

            db.Entry(locationItemType).State = EntityState.Modified;
            db.SaveChanges();

            TempData["LocationItemStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        public JsonResult IsAlreadyExists(int numLocationID, int numItemTypeID)
        {
            return Json(IsAvailable(numLocationID, numItemTypeID), JsonRequestBehavior.AllowGet);
        }

        public bool IsAvailable(int numLocationID, int numItemTypeID)
        {
            bool status = false;
            if (_loiStatus == false)
            {
                var locationItemType = (from E in db.LocationItemTypes where E.numLocationID == numLocationID && E.numItemTypeID == numItemTypeID && E.bitActive == true select new { numItemTypeID }).FirstOrDefault();
                if (locationItemType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_loiUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var locationItemType = (from E in db.LocationItemTypes where E.numLocationItemTypeID != _loiRecId && E.numLocationID == numLocationID && E.numItemTypeID == numItemTypeID && E.bitActive == true select new { numItemTypeID }).FirstOrDefault();
                    if (locationItemType != null)
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
            if (_loiUserId == Convert.ToInt32(Session["UserID"]) && _loiRecId == id)
            {
                _loiRecId = -1;
                _loiStatus = false;
                _loiUserId = -1;
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