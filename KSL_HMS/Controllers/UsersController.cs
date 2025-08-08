using KSL_HMS.DB;
using KSL_HMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class UsersController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _usrUserId = -1;
        public static int _usrRecId = -1;
        public static bool _usrStatus = false;

        public ActionResult Login()
        {
            if (Session["UserID"] != null)
            {
                Session.Clear();
                Session.RemoveAll();
                Session.Abandon();
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(User user)
        {
            try
            {
                var UserCount = db.Users.Count(u => u.varUserName.ToUpper() == user.varUserName.ToUpper() && u.bitActive == true);
                if (UserCount == 0)
                {
                    ViewBag.UserNoExistError = "User Not Exist.";
                }
                else
                {
                    var Luser = db.Users.Where(u => u.varUserName.ToUpper() == user.varUserName.ToUpper() && u.bitActive == true).First();

                    if (DecryptPassword(Luser.varPassword).ToUpper() == user.varPassword.ToUpper())
                    {
                        Session["UserID"] = Luser.numUserID.ToString();
                        Session["UserName"] = Luser.varFirstName + " " + Luser.varLastName;
                        Session["UserNIC"] = Luser.varNIC;
                        Session["UserPermissionID"] = Luser.numPermissionID;
                        Session["UserPermission"] = Luser.Permission.varPermissionName;

                        if (Luser.Permission.varPermissionName == "Administrator" || Luser.Permission.varPermissionName == "Finance")
                        {
                            return RedirectToAction("Dashboard");
                        }
                        else if (Luser.Permission.varPermissionName == "Bar")
                        {
                            Session["BarLocationID"] = 2; //HardCoded Bar Location ID
                            return RedirectToAction("AddToBar", "BillDetails");
                        }
                        else if (Luser.Permission.varPermissionName == "Training")
                        {
                            return RedirectToAction("Create", "KiteTrainingDetails");
                        }
                        else if (Luser.Permission.varPermissionName == "Rental")
                        {
                            return RedirectToAction("Create", "KiteRentalDetails");
                        }
                        else if (Luser.Permission.varPermissionName == "Repair")
                        {
                            Session["RepairShopLocationID"] = 1; //HardCoded Repair Shop Location ID
                            return RedirectToAction("AddToRepairShop", "BillDetails");
                        }
                        else
                        {
                            Session["ShopLocationID"] = 6; //HardCoded Shop Location ID
                            return RedirectToAction("AddToShop", "BillDetails");
                        }
                    }
                    else
                    {
                        ViewBag.UserPwdError = "Invalid Password.";
                    }
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Eror", new { Msg = ex.Message.ToString() });
            }
            return View(user);
        }

        public ActionResult Eror(string Msg)
        {
            ViewBag.Msg = Msg;
            return View();
        }

        public ActionResult Register()
        {
            ViewBag.numPermissionID = new SelectList(db.Permissions, "numPermissionID", "varPermissionName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(User user)
        {
            var UsrNic = db.Users.Count(u => u.varNIC.ToUpper() == user.varNIC.ToUpper());
            var UsrName = db.Users.Count(u => u.varUserName.ToUpper() == user.varUserName.ToUpper());
            if (UsrNic == 1)
            {
                ViewBag.UserNoEmpExiError = "Employee Already Exists.";
            }
            else if (UsrName == 1)
            {
                ViewBag.UserExistError = "User Already Exists.";
            }
            else
            {
                user.varPassword = EncryptPassword(user.varPassword).ToString();
                user.dtCreatedDate = System.DateTime.Now;
                user.numPermissionID = db.Permissions.Where(p => p.varPermissionName == "N/A" && p.bitActive == true).Select(p => p.numPermissionID).FirstOrDefault();
                db.Users.Add(user);
                db.SaveChanges();

                TempData["SucessMsg"] = "Welcome";
                return RedirectToAction("Login");
            }
            return View(user);
        }

        public string EncryptPassword(string Password)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] clearBytes = Encoding.Unicode.GetBytes(Password);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    Password = Convert.ToBase64String(ms.ToArray());
                }
            }
            return Password;
        }

        public string DecryptPassword(string Password)
        {
            string EncryptionKey = "MAKV2SPBNI99212";
            byte[] cipherBytes = Convert.FromBase64String(Password);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    Password = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
            return Password;
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varUserName)
        {
            return Json(IsAvailable(varUserName));
        }

        public bool IsAvailable(string varUserName)
        {
            bool status = false;
            if (_usrStatus == false)
            {
                var name = (from E in db.Users where E.varUserName.ToUpper() == varUserName.ToUpper() && E.bitActive == true select new { varUserName }).FirstOrDefault();
                if (name != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_usrUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var name = (from E in db.Users where E.numUserID != _usrRecId && E.varUserName.ToUpper() == varUserName.ToUpper() && E.bitActive == true select new { varUserName }).FirstOrDefault();
                    if (name != null)
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

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _usrRecId = -1;
                _usrStatus = false;
                var users = db.Users.Include(u => u.Permission).OrderByDescending(x => x.dtCreatedDate);
                return View(users.ToList());
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Dashboard()
        {
            if (Session["UserID"] != null)
            {
                List<CalendarResource> calendarResources = new List<CalendarResource>();
                var Rooms = db.Rooms.Where(r => r.bitActive == true && r.RoomType.varRoomTypeName != "External" && r.RoomType.varRoomTypeName != "Hired Room").OrderBy(r => r.RoomType.varRoomTypeName).ToList();
                foreach (var room in Rooms)
                {
                    calendarResources.Add(new CalendarResource
                    {
                        id = room.numRoomID,
                        category = room.RoomType.varRoomTypeName,
                        title = room.varRoomNo + " - (" + room.numMaxPaxAllowed.Value.ToString("00") + ")"
                    });
                }
                TempData["CalendarResource"] = calendarResources.OrderBy(r => r.category).ToList();

                List<CalendarEvent> calendarEvent = new List<CalendarEvent>();
                List<BookingDetail> _bookingDetail = db.BookingDetails.Where(bd => bd.bitActive == true).ToList();
                var Bookings = db.BookingHeaders.Where(bh => bh.bitActive == true && bh.BookingRefference.bitExternalBooking == false).ToList();
                var backgroundColor = "";
                var textColor = "";
                var borderColor = "";
                foreach (var Booking in Bookings)
                {
                    int numPax = Convert.ToInt32(Booking.numAdultsCount) + Convert.ToInt32(Booking.numChildrensCount) + Convert.ToInt32(Booking.numInfantsCount);

                    if (Booking.BookingRefference.bitClosed == true)
                    {
                        backgroundColor = "#ffe0db";
                        textColor = "#ff3e1d";
                        borderColor = "#ffc5bb";
                    }
                    else if (Booking.bitCheckedOut == true)
                    {
                        backgroundColor = "#fff2d6";
                        textColor = "#ffab00";
                        borderColor = "#ffe6b3";
                    }
                    else if (Booking.bitCheckedIn == true)
                    {
                        backgroundColor = "#e8fadf";
                        textColor = "#71dd37";
                        borderColor = "#d4f5c3";
                    }
                    else
                    {
                        backgroundColor = "#e7e7ff";
                        textColor = "#696cff";
                        borderColor = "#d2d3ff";
                    }

                    calendarEvent.Add(new CalendarEvent
                    {
                        id = Booking.numBookingHeaderID,
                        resourceId = Booking.numRoomID.Value.ToString(),
                        start = Booking.dtFromDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        end = Booking.dtToDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"),
                        title = numPax.ToString("00"),
                        backgroundColor = backgroundColor,
                        textColor = textColor,
                        borderColor = borderColor
                    });
                }
                TempData["CalendarEvent"] = calendarEvent.ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Create()
        {
            if (Session["UserID"] != null)
            {
                _usrRecId = -1;
                _usrStatus = false;
                ViewBag.numPermissionID = new SelectList(db.Permissions.Where(p => p.bitActive == true).OrderBy(x => x.varPermissionName), "numPermissionID", "varPermissionName");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(User user)
        {
            var UsrNic = db.Users.Count(u => u.varNIC.ToUpper() == user.varNIC.ToUpper());
            if (UsrNic == 1)
            {
                ViewBag.UserNoEmpExiError = "NIC Already Exists.";
            }
            else
            {
                user.varPassword = EncryptPassword(user.varPassword).ToString();
                user.bitActive = true;
                user.dtCreatedDate = System.DateTime.Now;
                db.Users.Add(user);
                db.SaveChanges();

                TempData["UserStatus"] = "Saved";
                return RedirectToAction("Index");
            }
            ViewBag.numPermissionID = new SelectList(db.Permissions.Where(p => p.bitActive == true), "numPermissionID", "varPermissionName", user.numPermissionID);
            return View(user);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            _usrRecId = user.numUserID;
            _usrStatus = true;
            _usrUserId = Convert.ToInt32(Session["UserID"]);
            user.varPassword = DecryptPassword(user.varPassword).ToString();

            ViewBag.numPermissionID = new SelectList(db.Permissions.Where(p => p.bitActive == true).OrderBy(x => x.varPermissionName), "numPermissionID", "varPermissionName", user.numPermissionID);
            return PartialView("_User", db.Users.Where(c => c.numUserID == id).FirstOrDefault<User>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(User user)
        {
            user.varPassword = EncryptPassword(user.varPassword).ToString();
            user.bitActive = true;
            user.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            user.dtUpdatetDate = System.DateTime.Now;

            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            _usrRecId = -1;
            _usrStatus = false;

            TempData["UserStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            user.bitActive = false;
            user.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            user.dtDeletedDate = System.DateTime.Now;

            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();

            TempData["UserStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        public ActionResult Maintenance()
        {
            return View();
        }

        public ActionResult ChangeValidationStatus(int? id)
        {
            if (_usrUserId == Convert.ToInt32(Session["UserID"]) && _usrRecId == id)
            {
                _usrRecId = -1;
                _usrStatus = false;
                _usrUserId = -1;
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