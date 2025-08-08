using KSL_HMS.DB;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class RoomCheckInOutsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _rmtUserId = -1;
        public static int _rmtRecId = -1;
        public static bool _rmtStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _rmtRecId = -1;
                _rmtStatus = false;
                ViewBag.RoomCheckInOuts = db.RoomCheckInOuts.Where(i => i.bitActive == true).OrderBy(x => x.varRoomCheckInTime).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RoomCheckInOut roomCheckInOut)
        {
            roomCheckInOut.bitActive = true;
            roomCheckInOut.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            roomCheckInOut.dtCreatedDate = System.DateTime.Now;
            db.RoomCheckInOuts.Add(roomCheckInOut);
            db.SaveChanges();

            TempData["RoomCheckInOutStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RoomCheckInOut roomCheckInOut = db.RoomCheckInOuts.Find(id);
            if (roomCheckInOut == null)
            {
                return HttpNotFound();
            }
            _rmtRecId = roomCheckInOut.numRoomCheckInOutID;
            _rmtStatus = true;
            _rmtUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_RoomCheckInOut", db.RoomCheckInOuts.Where(c => c.numRoomCheckInOutID == id).FirstOrDefault<RoomCheckInOut>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RoomCheckInOut roomCheckInOut)
        {
            roomCheckInOut.bitActive = true;
            roomCheckInOut.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            roomCheckInOut.dtUpdatetDate = System.DateTime.Now;

            db.Entry(roomCheckInOut).State = EntityState.Modified;
            db.SaveChanges();

            _rmtRecId = -1;
            _rmtStatus = false;

            TempData["RoomCheckInOutStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RoomCheckInOut roomCheckInOut = db.RoomCheckInOuts.Find(id);
            if (roomCheckInOut == null)
            {
                return HttpNotFound();
            }

            roomCheckInOut.bitActive = false;
            roomCheckInOut.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            roomCheckInOut.dtDeletedDate = System.DateTime.Now;

            db.Entry(roomCheckInOut).State = EntityState.Modified;
            db.SaveChanges();

            TempData["RoomCheckInOutStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        public ActionResult ChangeValidationStatus(int? id)
        {
            if (_rmtUserId == Convert.ToInt32(Session["UserID"]) && _rmtRecId == id)
            {
                _rmtRecId = -1;
                _rmtStatus = false;
                _rmtUserId = -1;
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