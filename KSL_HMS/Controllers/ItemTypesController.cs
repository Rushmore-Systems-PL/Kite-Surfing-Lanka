using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class ItemTypesController : Controller
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
                ViewBag.ItemTypes = db.ItemTypes.Where(i => i.bitActive == true).OrderBy(x => x.varItemTypeName).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ItemType itemType)
        {
            itemType.bitActive = true;
            itemType.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
            itemType.dtCreatedDate = System.DateTime.Now;

            db.ItemTypes.Add(itemType);
            db.SaveChanges();

            TempData["ItemTypeStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ItemType itemType = db.ItemTypes.Find(id);
            if (itemType == null)
            {
                return HttpNotFound();
            }
            _ittRecId = itemType.numItemTypeID;
            _ittStatus = true;
            _ittUserId = Convert.ToInt32(Session["UserID"]);

            return PartialView("_ItemType", db.ItemTypes.Where(c => c.numItemTypeID == id).FirstOrDefault<ItemType>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ItemType itemType)
        {
            itemType.bitActive = true;
            itemType.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            itemType.dtUpdatetDate = System.DateTime.Now;

            db.Entry(itemType).State = EntityState.Modified;
            db.SaveChanges();

            _ittRecId = -1;
            _ittStatus = false;

            TempData["ItemTypeStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ItemType itemType = db.ItemTypes.Find(id);
            if (itemType == null)
            {
                return HttpNotFound();
            }

            itemType.bitActive = false;
            itemType.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            itemType.dtDeletedDate = System.DateTime.Now;

            db.Entry(itemType).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ItemTypeStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string varItemTypeName)
        {
            return Json(IsAvailable(varItemTypeName));
        }

        public bool IsAvailable(string varItemTypeName)
        {
            bool status = false;
            if (_ittStatus == false)
            {
                var ItemType = (from E in db.ItemTypes where E.varItemTypeName.ToUpper() == varItemTypeName.ToUpper() && E.bitActive == true select new { varItemTypeName }).FirstOrDefault();
                if (ItemType != null)
                { status = false; }
                else
                { status = true; }
                return status;
            }
            else
            {
                if (_ittUserId == Convert.ToInt32(Session["UserID"]))
                {
                    var ItemType = (from E in db.ItemTypes where E.numItemTypeID != _ittRecId && E.varItemTypeName.ToUpper() == varItemTypeName.ToUpper() && E.bitActive == true select new { varItemTypeName }).FirstOrDefault();
                    if (ItemType != null)
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