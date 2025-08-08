using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class CurrenciesController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                return View(db.Currencies.Where(c => c.bitActive == true).OrderBy(x => x.varCurrencyName).ToList());
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection formCollection)
        {
            var Id = formCollection.GetValues("numCurrencyID");
            var Name = formCollection.GetValues("varCurrencyName");
            var Code = formCollection.GetValues("varCurrencyCode");
            var Rate = formCollection.GetValues("numRate");

            for (int i = 0; i < Id.Count(); i++)
            {
                if (!string.IsNullOrEmpty(Id[i]) && !string.IsNullOrEmpty(Name[i]) && !string.IsNullOrEmpty(Code[i]) && !string.IsNullOrEmpty(Rate[i]))
                {
                    if (Id[i] == "-1")
                    {
                        Currency newCurrency = new Currency();
                        newCurrency.varCurrencyName = Name[i];
                        newCurrency.varCurrencyCode = Code[i].ToUpper();
                        newCurrency.numRate = Convert.ToDecimal(Rate[i]);
                        newCurrency.bitActive = true;
                        newCurrency.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
                        newCurrency.dtCreatedDate = System.DateTime.Now;

                        db.Currencies.Add(newCurrency);
                        db.SaveChanges();
                    }
                    else
                    {
                        Currency exCurrency = db.Currencies.Find(Convert.ToInt32(Id[i]));
                        exCurrency.varCurrencyName = Name[i];
                        exCurrency.varCurrencyCode = Code[i].ToUpper();
                        exCurrency.numRate = Convert.ToDecimal(Rate[i]);
                        exCurrency.bitActive = true;
                        exCurrency.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
                        exCurrency.dtUpdatetDate = System.DateTime.Now;

                        db.Entry(exCurrency).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            TempData["CurrencyStatus"] = "Saved";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult IsAlreadyExists(string Name, string Code)
        {
            return Json(IsAvailable(Name, Code));
        }

        public bool IsAvailable(string Name, string Code)
        {
            bool status = false;
            var Currency = (from E in db.Currencies where E.varCurrencyCode.ToUpper() == Code.ToUpper() && E.varCurrencyName.ToUpper() == Name.ToUpper() && E.bitActive == true select new { Code }).FirstOrDefault();
            if (Currency != null)
            { status = false; }
            else
            { status = true; }
            return status;
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Currency currency = db.Currencies.Find(id);
            if (currency == null)
            {
                return HttpNotFound();
            }

            currency.bitActive = false;
            currency.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            currency.dtDeletedDate = System.DateTime.Now;

            db.Entry(currency).State = EntityState.Modified;
            db.SaveChanges();

            TempData["CurrencyStatus"] = "Deleted";
            return RedirectToAction("Index");
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