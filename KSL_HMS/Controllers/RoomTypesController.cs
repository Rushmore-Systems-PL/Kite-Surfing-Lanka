using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class RoomTypesController : Controller
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
                ViewBag.RoomTypes = db.RoomTypes.Where(i => i.bitActive == true).OrderBy(x => x.varRoomTypeName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RoomType roomType)
        {
            roomType.bitActive = true;
            roomType.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            roomType.dtCreatedDate = System.DateTime.Now;
            db.RoomTypes.Add(roomType);
            db.SaveChanges();

            if (roomType.RoomCategoryImage != null)
            {
                string ftpServerUrl = "ftp://ksloffice.com/Images/" + roomType.numRoomTypeID.ToString() + ".png";
                FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(ftpServerUrl);
                ftpWebRequest.Credentials = new NetworkCredential("ph14046620239", "KSLoffice123!@#");
                ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpWebRequest.UseBinary = true;

                using (Stream ftpStream = ftpWebRequest.GetRequestStream())
                {
                    Request.Files[0].InputStream.CopyTo(ftpStream);
                }

                FtpWebResponse ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                string responseMessage = ftpWebResponse.StatusDescription;
                ftpWebResponse.Close();
            }

            TempData["RoomTypeStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RoomType roomType = db.RoomTypes.Find(id);
            if (roomType == null)
            {
                return HttpNotFound();
            }
            _rmtRecId = roomType.numRoomTypeID;
            _rmtStatus = true;
            _rmtUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_RoomType", db.RoomTypes.Where(c => c.numRoomTypeID == id).FirstOrDefault<RoomType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RoomType roomType)
        {
            roomType.bitActive = true;
            roomType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            roomType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(roomType).State = EntityState.Modified;
            db.SaveChanges();

            if (roomType.RoomCategoryImage != null)
            {
                string ftpUrl = $"ftp://ksloffice.com/Images/{roomType.numRoomTypeID}.png";
                string ftpUsername = "ph14046620239";
                string ftpPassword = "KSLoffice123!@#";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                request.Method = WebRequestMethods.Ftp.DeleteFile;

                string ftpServerUrl = "ftp://ksloffice.com/Images/" + roomType.numRoomTypeID.ToString() + ".png";
                FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(ftpServerUrl);
                ftpWebRequest.Credentials = new NetworkCredential("ph14046620239", "KSLoffice123!@#");
                ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
                ftpWebRequest.UseBinary = true;

                using (Stream ftpStream = ftpWebRequest.GetRequestStream())
                {
                    Request.Files[0].InputStream.CopyTo(ftpStream);
                }

                FtpWebResponse ftpWebResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                string responseMessage = ftpWebResponse.StatusDescription;
                ftpWebResponse.Close();
            }

            _rmtRecId = -1;
            _rmtStatus = false;

            TempData["RoomTypeStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RoomType roomType = db.RoomTypes.Find(id);
            if (roomType == null)
            {
                return HttpNotFound();
            }

            roomType.bitActive = false;
            roomType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            roomType.dtDeletedDate = System.DateTime.Now;

            db.Entry(roomType).State = EntityState.Modified;
            db.SaveChanges();

            string ftpUrl = $"ftp://ksloffice.com/Images/{roomType.numRoomTypeID}.png";
            string ftpUsername = "ph14046620239";
            string ftpPassword = "KSLoffice123!@#";
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
            request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
            request.Method = WebRequestMethods.Ftp.DeleteFile;

            TempData["RoomTypeStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varRoomTypeName)
        {
            return Json(IsAvailable(varRoomTypeName));
        }

        public bool IsAvailable(string varRoomTypeName)
        {
            bool status = false;
            if (_rmtStatus == false)
            {
                var RoomType = (from E in db.RoomTypes where E.varRoomTypeName.ToUpper() == varRoomTypeName.ToUpper() && E.bitActive == true select new { varRoomTypeName }).FirstOrDefault();
                if (RoomType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_rmtUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var RoomType = (from E in db.RoomTypes where E.numRoomTypeID != _rmtRecId && E.varRoomTypeName.ToUpper() == varRoomTypeName.ToUpper() && E.bitActive == true select new { varRoomTypeName }).FirstOrDefault();
                    if (RoomType != null)
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

        public ActionResult Image(int id)
        {
            try
            {
                WebClient client = new WebClient();
                client.Credentials = new NetworkCredential("ph14046620239", "KSLoffice123!@#");
                byte[] imageBytes = client.DownloadData($"ftp://ksloffice.com/Images/{id}.png");
                return File(imageBytes, "image/png");
            }
            catch (Exception ex)
            {
                WebClient client = new WebClient();
                client.Credentials = new NetworkCredential("ph14046620239", "KSLoffice123!@#");
                byte[] imageBytes = client.DownloadData($"ftp://ksloffice.com/Images/NORT.png");
                return File(imageBytes, "image/png");
            }
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