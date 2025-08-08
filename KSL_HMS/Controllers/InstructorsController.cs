using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class InstructorsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _insUserId = -1;
        public static int _insRecId = -1;
        public static bool _insStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _insRecId = -1;
                _insStatus = false;
                ViewBag.Instructors = db.Instructors.Where(i => i.bitActive == true).OrderBy(x => x.varInstructorName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Instructor instructor)
        {
            instructor.bitActive = true;
            instructor.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            instructor.dtCreatedDate = System.DateTime.Now;

            db.Instructors.Add(instructor);
            db.SaveChanges();

            TempData["InstructorStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructors.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }
            _insRecId = instructor.numInstructorID;
            _insStatus = true;
            _insUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_Instructor", db.Instructors.Where(c => c.numInstructorID == id).FirstOrDefault<Instructor>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Instructor instructor)
        {
            instructor.bitActive = true;
            instructor.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            instructor.dtUpdatetDate = System.DateTime.Now;

            db.Entry(instructor).State = EntityState.Modified;
            db.SaveChanges();

            _insRecId = -1;
            _insStatus = false;

            TempData["InstructorStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Instructor instructor = db.Instructors.Find(id);
            if (instructor == null)
            {
                return HttpNotFound();
            }

            instructor.bitActive = false;
            instructor.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            instructor.dtDeletedDate = System.DateTime.Now;

            db.Entry(instructor).State = EntityState.Modified;
            db.SaveChanges();

            TempData["InstructorStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varInstructorNIC)
        {
            return Json(IsAvailable(varInstructorNIC));
        }

        public bool IsAvailable(string varInstructorNIC)
        {
            bool status = false;
            if (_insStatus == false)
            {
                var Instructor = (from E in db.Instructors where E.varInstructorNIC.ToUpper() == varInstructorNIC.ToUpper() && E.bitActive == true select new { varInstructorNIC }).FirstOrDefault();
                if (Instructor != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_insUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var Instructor = (from E in db.Instructors where E.numInstructorID != _insRecId && E.varInstructorNIC.ToUpper() == varInstructorNIC.ToUpper() && E.bitActive == true select new { varInstructorNIC }).FirstOrDefault();
                    if (Instructor != null)
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
            if (_insUserId == Convert.ToInt32(Session["UserID"]) && _insRecId == id)
            {
                _insRecId = -1;
                _insStatus = false;
                _insUserId = -1;
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