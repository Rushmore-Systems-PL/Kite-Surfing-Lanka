using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class RoomTypeRatesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _rtrUserId = -1;
        public static int _rtrRecId = -1;
        public static bool _rtrStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _rtrRecId = -1;
                _rtrStatus = false;
                ViewBag.numRoomTypeID = new SelectList(db.RoomTypes.Where(t => t.bitActive == true).OrderBy(x => x.varRoomTypeName), "numRoomTypeID", "varRoomTypeName");
                ViewBag.RoomTypeRates = db.RoomTypeRates.Where(i => i.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RoomTypeRate roomTypeRate)
        {
            roomTypeRate.bitActive = true;
            roomTypeRate.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            roomTypeRate.dtCreatedDate = System.DateTime.Now;

            db.RoomTypeRates.Add(roomTypeRate);
            db.SaveChanges();

            TempData["RoomTypeRatestatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RoomTypeRate roomTypeRate = db.RoomTypeRates.Find(id);
            if (roomTypeRate == null)
            {
                return HttpNotFound();
            }
            _rtrRecId = roomTypeRate.numRoomTypeRateID;
            _rtrStatus = true;
            _rtrUserId = Convert.ToInt32(Session["UserID"]);

            ViewBag.numRoomTypeID = new SelectList(db.RoomTypes.Where(t => t.bitActive == true).OrderBy(x => x.varRoomTypeName), "numRoomTypeID", "varRoomTypeName", roomTypeRate.numRoomTypeRateID);
            return PartialView("_RoomTypeRate", db.RoomTypeRates.Where(c => c.numRoomTypeRateID == id).FirstOrDefault<RoomTypeRate>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RoomTypeRate roomTypeRate)
        {
            roomTypeRate.bitActive = true;
            roomTypeRate.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            roomTypeRate.dtUpdatetDate = System.DateTime.Now;

            db.Entry(roomTypeRate).State = EntityState.Modified;
            db.SaveChanges();

            _rtrRecId = -1;
            _rtrStatus = false;

            TempData["RoomTypeRatestatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RoomTypeRate roomTypeRate = db.RoomTypeRates.Find(id);
            if (roomTypeRate == null)
            {
                return HttpNotFound();
            }

            roomTypeRate.bitActive = false;
            roomTypeRate.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            roomTypeRate.dtDeletedDate = System.DateTime.Now;

            db.Entry(roomTypeRate).State = EntityState.Modified;
            db.SaveChanges();

            TempData["RoomTypeRatestatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(decimal numPersonCount, int numRoomTypeID)
        {
            return Json(IsAvailable(numPersonCount, numRoomTypeID));
        }

        public bool IsAvailable(decimal numPersonCount, int numRoomTypeID)
        {
            bool status = false;
            if (_rtrStatus == false)
            {
                var RoomTypeRate = (from E in db.RoomTypeRates where E.numRoomTypeID == numRoomTypeID && E.numPersonCount == numPersonCount && E.bitActive == true select new { numPersonCount }).FirstOrDefault();
                if (RoomTypeRate != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_rtrUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var RoomTypeRate = (from E in db.RoomTypeRates where E.numRoomTypeRateID != _rtrRecId && E.numRoomTypeID == numRoomTypeID && E.numPersonCount == numPersonCount && E.bitActive == true select new { numPersonCount }).FirstOrDefault();
                    if (RoomTypeRate != null)
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
            if (_rtrUserId == Convert.ToInt32(Session["UserID"]) && _rtrRecId == id)
            {
                _rtrRecId = -1;
                _rtrStatus = false;
                _rtrUserId = -1;
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