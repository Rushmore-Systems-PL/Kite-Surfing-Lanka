using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class ItemsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _ittUserId = -1;
        public static int _ittRecId = -1;
        public static bool _ittStatus = false;

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _ittRecId = -1;
                _ittStatus = false;
                ViewBag.numItemTypeID = new SelectList(db.ItemTypes.Where(i => i.bitActive == true).OrderBy(x => x.varItemTypeName), "numItemTypeID", "varItemTypeName");
                ViewBag.Items = db.Items.Where(i => i.bitIsDeleted == false).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Item item)
        {
            item.bitIsDeleted = false;
            item.bitActive = true;
            item.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            item.dtCreatedDate = System.DateTime.Now;

            db.Items.Add(item);
            db.SaveChanges();

            TempData["ItemStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Item item = db.Items.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }
            _ittRecId = item.numItemID;
            _ittStatus = true;
            _ittUserId = Convert.ToInt32(Session["UserID"]);

            ViewBag.numItemTypeID = new SelectList(db.ItemTypes.Where(i => i.bitActive == true).OrderBy(x => x.varItemTypeName), "numItemTypeID", "varItemTypeName", item.numItemTypeID);
            return PartialView("_Item", db.Items.Where(c => c.numItemID == id).FirstOrDefault<Item>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Item item)
        {
            item.bitIsDeleted = false;
            item.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            item.dtUpdatetDate = System.DateTime.Now;

            db.Entry(item).State = EntityState.Modified;
            db.SaveChanges();

            _ittRecId = -1;
            _ittStatus = false;

            TempData["ItemStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Item item = db.Items.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            item.bitIsDeleted = true;
            item.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            item.dtDeletedDate = System.DateTime.Now;

            db.Entry(item).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ItemStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        public ActionResult StatusChange(int? id, bool? bit)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Item item = db.Items.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            item.bitActive = bit;
            item.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            item.dtUpdatetDate = System.DateTime.Now;

            db.Entry(item).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ItemStatus"] = "Changed";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varItemName)
        {
            return Json(IsAvailable(varItemName));
        }

        public bool IsAvailable(string varItemName)
        {
            bool status = false;
            if (_ittStatus == false)
            {
                var Item = (from E in db.Items where E.varItemName.ToUpper() == varItemName.ToUpper() && E.bitIsDeleted == false select new { varItemName }).FirstOrDefault();
                if (Item != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_ittUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var Item = (from E in db.Items where E.numItemID != _ittRecId && E.varItemName.ToUpper() == varItemName.ToUpper() && E.bitIsDeleted == false select new { varItemName }).FirstOrDefault();
                    if (Item != null)
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
            if (_ittUserId == Convert.ToInt32(Session["UserID"]) && _ittRecId == id)
            {
                _ittRecId = -1;
                _ittStatus = false;
                _ittUserId = -1;
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