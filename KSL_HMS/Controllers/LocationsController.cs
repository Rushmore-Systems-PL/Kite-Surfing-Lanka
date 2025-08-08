using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class LocationsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _locUserId = -1;
        public static int _locRecId = -1;
        public static bool _locStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _locRecId = -1;
                _locStatus = false;
                ViewBag.Locations = db.Locations.Where(i => i.bitActive == true).OrderBy(x => x.varLocationName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Location location)
        {
            location.bitActive = true;
            location.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            location.dtCreatedDate = System.DateTime.Now;

            db.Locations.Add(location);
            db.SaveChanges();

            TempData["LocationStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Location location = db.Locations.Find(id);
            if (location == null)
            {
                return HttpNotFound();
            }
            _locRecId = location.numLocationID;
            _locStatus = true;
            _locUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_Location", db.Locations.Where(c => c.numLocationID == id).FirstOrDefault<Location>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Location location)
        {
            location.bitActive = true;
            location.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            location.dtUpdatetDate = System.DateTime.Now;

            db.Entry(location).State = EntityState.Modified;
            db.SaveChanges();

            _locRecId = -1;
            _locStatus = false;

            TempData["LocationStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Location location = db.Locations.Find(id);
            if (location == null)
            {
                return HttpNotFound();
            }

            location.bitActive = false;
            location.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            location.dtDeletedDate = System.DateTime.Now;

            db.Entry(location).State = EntityState.Modified;
            db.SaveChanges();

            TempData["LocationStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varLocationName)
        {
            return Json(IsAvailable(varLocationName));
        }

        public bool IsAvailable(string varLocationName)
        {
            bool status = false;
            if (_locStatus == false)
            {
                var location = (from E in db.Locations where E.varLocationName.ToUpper() == varLocationName.ToUpper() && E.bitActive == true select new { varLocationName }).FirstOrDefault();
                if (location != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_locUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var location = (from E in db.Locations where E.numLocationID != _locRecId && E.varLocationName.ToUpper() == varLocationName.ToUpper() && E.bitActive == true select new { varLocationName }).FirstOrDefault();
                    if (location != null)
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
            if (_locUserId == Convert.ToInt32(Session["UserID"]) && _locRecId == id)
            {
                _locRecId = -1;
                _locStatus = false;
                _locUserId = -1;
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