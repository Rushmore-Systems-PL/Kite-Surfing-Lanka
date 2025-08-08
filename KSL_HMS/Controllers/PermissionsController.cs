using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class PermissionsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _pmnUserId = -1;
        public static int _pmnRecId = -1;
        public static bool _pmnStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _pmnRecId = -1;
                _pmnStatus = false;
                ViewBag.Permissions = db.Permissions.Where(i => i.bitActive == true).OrderBy(x => x.varPermissionName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Permission permission)
        {
            permission.bitActive = true;
            permission.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            permission.dtCreatedDate = System.DateTime.Now;

            db.Permissions.Add(permission);
            db.SaveChanges();

            TempData["PermissionStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Permission permission = db.Permissions.Find(id);
            if (permission == null)
            {
                return HttpNotFound();
            }
            _pmnRecId = permission.numPermissionID;
            _pmnStatus = true;
            _pmnUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_Permission", db.Permissions.Where(c => c.numPermissionID == id).FirstOrDefault<Permission>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Permission permission)
        {
            permission.bitActive = true;
            permission.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            permission.dtUpdatetDate = System.DateTime.Now;

            db.Entry(permission).State = EntityState.Modified;
            db.SaveChanges();

            _pmnRecId = -1;
            _pmnStatus = false;

            TempData["PermissionStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Permission permission = db.Permissions.Find(id);
            if (permission == null)
            {
                return HttpNotFound();
            }

            permission.bitActive = false;
            permission.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            permission.dtDeletedDate = System.DateTime.Now;

            db.Entry(permission).State = EntityState.Modified;
            db.SaveChanges();

            TempData["PermissionStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varPermissionName)
        {
            return Json(IsAvailable(varPermissionName));
        }

        public bool IsAvailable(string varPermissionName)
        {
            bool status = false;
            if (_pmnStatus == false)
            {
                var Permission = (from E in db.Permissions where E.varPermissionName.ToUpper() == varPermissionName.ToUpper() && E.bitActive == true select new { varPermissionName }).FirstOrDefault();
                if (Permission != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_pmnUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var Permission = (from E in db.Permissions where E.numPermissionID != _pmnRecId && E.varPermissionName.ToUpper() == varPermissionName.ToUpper() && E.bitActive == true select new { varPermissionName }).FirstOrDefault();
                    if (Permission != null)
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
            if (_pmnUserId == Convert.ToInt32(Session["UserID"]) && _pmnRecId == id)
            {
                _pmnRecId = -1;
                _pmnStatus = false;
                _pmnUserId = -1;
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