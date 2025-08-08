using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class RoomsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _romUserId = -1;
        public static int _romRecId = -1;
        public static bool _romStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _romRecId = -1;
                _romStatus = false;
                ViewBag.numRoomTypeID = new SelectList(db.RoomTypes.Where(i => i.bitActive == true).OrderBy(x => x.varRoomTypeName), "numRoomTypeID", "varRoomTypeName");
                ViewBag.Rooms = db.Rooms.Where(i => i.bitActive == true).OrderBy(x => x.varRoomNo).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Room room)
        {
            room.bitActive = true;
            room.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            room.dtCreatedDate = System.DateTime.Now;

            db.Rooms.Add(room);
            db.SaveChanges();

            TempData["RoomStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Room room = db.Rooms.Find(id);
            if (room == null)
            {
                return HttpNotFound();
            }
            _romRecId = room.numRoomID;
            _romStatus = true;
            _romUserId = Convert.ToInt32(Session["UserID"]);

            ViewBag.numRoomTypeID = new SelectList(db.RoomTypes.Where(i => i.bitActive == true).OrderBy(x => x.varRoomTypeName), "numRoomTypeID", "varRoomTypeName", room.numRoomTypeID);
            return PartialView("_Room", db.Rooms.Where(c => c.numRoomID == id).FirstOrDefault<Room>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Room room)
        {
            room.bitActive = true;
            room.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            room.dtUpdatetDate = System.DateTime.Now;

            db.Entry(room).State = EntityState.Modified;
            db.SaveChanges();

            _romRecId = -1;
            _romStatus = false;

            TempData["RoomStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Room room = db.Rooms.Find(id);
            if (room == null)
            {
                return HttpNotFound();
            }

            room.bitActive = false;
            room.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            room.dtDeletedDate = System.DateTime.Now;

            db.Entry(room).State = EntityState.Modified;
            db.SaveChanges();

            TempData["RoomStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varRoomNo)
        {
            return Json(IsAvailable(varRoomNo));
        }

        public bool IsAvailable(string varRoomNo)
        {
            bool status = false;
            if (_romStatus == false)
            {
                var Room = (from E in db.Rooms where E.varRoomNo.ToUpper() == varRoomNo.ToUpper() && E.bitActive == true select new { varRoomNo }).FirstOrDefault();
                if (Room != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_romUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var Room = (from E in db.Rooms where E.numRoomID != _romRecId && E.varRoomNo.ToString().ToUpper() == varRoomNo.ToString().ToUpper() && E.bitActive == true select new { varRoomNo }).FirstOrDefault();
                    if (Room != null)
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
            if (_romUserId == Convert.ToInt32(Session["UserID"]) && _romRecId == id)
            {
                _romRecId = -1;
                _romStatus = false;
                _romUserId = -1;
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