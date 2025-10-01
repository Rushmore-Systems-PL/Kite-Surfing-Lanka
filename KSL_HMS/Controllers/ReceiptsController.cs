using KSL_HMS.DB;
using KSL_HMS.Models;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class ReceiptsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();
        public static int _rptUserId = -1;
        public static int _rptRecId = -1;
        public static bool _rptStatus = false;

        public ActionResult Create()
        {
            if (Session["UserID"] != null)
            {
                _rptRecId = -1;
                _rptStatus = false;
                ViewBag.numBookingRefferenceID = new SelectList(db.BookingRefferences.Where(r => r.bitActive == true && r.bitClosed == false).OrderBy(x => x.dtCreatedDate), "numBookingRefferenceID", "varBookingRefferenceNo");
                var Bookings = (from bh in db.BookingHeaders
                                join bih in db.BillHeaders on bh.numBookingHeaderID equals bih.numBookingHeaderID
                                join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                                where bh.bitActive == true && bf.bitClosed == false
                                select new
                                {
                                    Bid = bh.numBookingHeaderID,
                                    Bdesc = "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo,
                                    Border = bh.dtCreatedDate
                                }).OrderBy(x => x.Border).ToList();
                ViewBag.numBookingHeaderID = new SelectList(Bookings, "Bid", "Bdesc");
                var Currency = db.Currencies.Where(c => c.bitActive == true).OrderBy(x => x.varCurrencyName).ToList();
                var DefaultCurrency = Currency.Where(c => c.varCurrencyCode == "EUR" && c.bitActive == true).First();//Defult Currency
                var Currencies = Currency.Where(c => c.bitActive == true).Select(x => new
                {
                    Cid = x.numCurrencyID,
                    Cdesc = x.varCurrencyName + " | " + x.varCurrencyCode + " | " + x.numRate,
                    Corder = x.varCurrencyName
                }).OrderBy(x => x.Corder).ToList();
                ViewBag.numCurrencyID = new SelectList(Currencies, "Cid", "Cdesc", DefaultCurrency.numCurrencyID);
                ViewBag.DefaultCurrencyRate = DefaultCurrency.numRate;
                ViewBag.Currencies = Currency.Select(x => new CurrencyRate { numID = x.numCurrencyID, numRate = x.numRate.Value }).ToList();
                ViewBag.numPaymentTypeID = new SelectList(db.PaymentTypes.Where(p => p.bitActive == true).OrderBy(x => x.varPaymentTypeName), "numPaymentTypeID", "varPaymentTypeName");
                ViewBag.numReciptTypeID = new SelectList(db.ReciptTypes.Where(p => p.bitActive == true).OrderBy(x => x.varReciptTypeName), "numReciptTypeID", "varReciptTypeName");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult LoadOutstandingAmount(int id, bool level)
        {
            decimal numOutAmount = 0;
            decimal totalAmount = 0;
            decimal paidAmount = 0;
            if (level == true)
            {
                numOutAmount = db.BillHeaders.Where(b => b.bitActive == true && b.numBookingHeaderID == id).Select(b => b.numBalanceToPay.Value).FirstOrDefault();
            }
            else
            {
                totalAmount = db.BillHeaders.Where(b => b.numBookingRefferenceID == id && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numTotalCost.Value).Sum();

                    paidAmount = db.Receipts.Where(b => b.numBookingRefferenceID == id && b.bitActive == true).Sum(r => (decimal?)r.numAmount) ?? 0;
                
                numOutAmount = totalAmount - paidAmount;
            }
            return Json(new { numOutAmount = numOutAmount }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult IsAmountValid(decimal numAmount, int? numBookingHeaderID, int? numBookingRefferenceID, bool bitSingleBooking)
        {
            return Json(IsAvailable(numAmount, numBookingHeaderID, numBookingRefferenceID, bitSingleBooking));
        }

        public bool IsAvailable(decimal numAmount, int? numBookingHeaderID, int? numBookingRefferenceID, bool bitSingleBooking)
        {
            bool status = false;
            if (_rptStatus == false)
            {
                if (bitSingleBooking == true)
                {
                    decimal numOutAmount = db.BillHeaders.Where(b => b.bitActive == true && b.numBookingHeaderID == numBookingHeaderID).Select(b => b.numBalanceToPay.Value).FirstOrDefault();
                    if (numAmount > numOutAmount)
                    { status = false; }
                    else
                    { status = true; }
                    return status;
                }
                else
                {
                    decimal numOutAmount = db.BillHeaders.Where(b => b.numBookingRefferenceID == numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(bh => bh.numBalanceToPay.Value).Sum();
                    if (numAmount > numOutAmount)
                    { status = false; }
                    else
                    { status = true; }
                    return status;
                }
            }
            else
            {
                if (_rptUserId == Convert.ToInt32(Session["UserID"]))
                {
                    Receipt receipt = db.Receipts.Where(r => r.numReceiptID == _rptRecId).FirstOrDefault();
                    if (receipt.bitSingleBooking == true)
                    {
                        BillHeader billHeader = db.BillHeaders.Where(b => b.numBillHeaderID == receipt.numBillHeaderID).FirstOrDefault();
                        decimal numOutStandingBeforPaid = billHeader.numBalanceToPay.Value + receipt.numAmount.Value;
                        if (numAmount > numOutStandingBeforPaid)
                        { status = false; }
                        else
                        { status = true; }
                        return status;
                    }
                    else
                    {
                        decimal numOutStandingBeforPaid = db.BillHeaders.Where(b => b.numBillHeaderID == receipt.numBillHeaderID && b.numBookingRefferenceID == numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(bh => bh.numBalanceToPay.Value).Sum();
                        if (numAmount > numOutStandingBeforPaid)
                        { status = false; }
                        else
                        { status = true; }
                        return status;
                    }
                }
                else
                {
                    return status;
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Receipt receipt)
        {
            if (receipt.bitSingleBooking == true)
            {
                receipt.numBookingRefferenceID = db.BookingHeaders.Where(h => h.numBookingHeaderID == receipt.numBookingHeaderID).Select(h => h.numBookingRefferenceID).FirstOrDefault();
                receipt.numBillHeaderID = db.BillHeaders.Where(b => b.bitActive == true && b.numBookingHeaderID == receipt.numBookingHeaderID).Select(b => b.numBillHeaderID).FirstOrDefault();
                var varBookingRefferenceNo = db.BookingRefferences.Where(b => b.numBookingRefferenceID == receipt.numBookingRefferenceID).Select(b => b.varBookingRefferenceNo).FirstOrDefault();

                int numRef = db.Receipts.Where(r => r.bitActive == true && r.numBookingRefferenceID == receipt.numBookingRefferenceID).Count();
                receipt.varReceiptNo = varBookingRefferenceNo + (numRef + 1).ToString("00");

                var DefaultCurrencyID = db.Currencies.Where(c => c.varCurrencyCode == "EUR" && c.bitActive == true).Select(c => c.numCurrencyID).First();//Defult Currency ID
                if (receipt.numCurrencyID == DefaultCurrencyID)
                {
                    receipt.bitIsDefaultCurrency = true;
                }
                else
                {
                    receipt.bitIsDefaultCurrency = false;
                }

                receipt.bitActive = true;
                receipt.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
                receipt.dtCreatedDate = System.DateTime.Now;
                db.Receipts.Add(receipt);
                db.SaveChanges();

                BillHeader billHeader = db.BillHeaders.Find(receipt.numBillHeaderID);
                billHeader.numPayedAmount = billHeader.numPayedAmount + receipt.numAmount;
                billHeader.numBalanceToPay = billHeader.numBalanceToPay - receipt.numAmount;
                db.Entry(billHeader).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                var varBookingRefferenceNo = db.BookingRefferences.Where(b => b.numBookingRefferenceID == receipt.numBookingRefferenceID).Select(b => b.varBookingRefferenceNo).FirstOrDefault();
                int numRef = db.Receipts.Where(r => r.bitActive == true && r.numBookingRefferenceID == receipt.numBookingRefferenceID).Count();
                receipt.varReceiptNo = varBookingRefferenceNo + (numRef + 1).ToString("00");

                var DefaultCurrencyID = db.Currencies.Where(c => c.varCurrencyCode == "EUR" && c.bitActive == true).Select(c => c.numCurrencyID).First();//Defult Currency ID
                if (receipt.numCurrencyID == DefaultCurrencyID)
                {
                    receipt.bitIsDefaultCurrency = true;
                }
                else
                {
                    receipt.bitIsDefaultCurrency = false;
                }

                receipt.bitActive = true;
                receipt.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
                receipt.dtCreatedDate = System.DateTime.Now;
                db.Receipts.Add(receipt);
                db.SaveChanges();

                decimal? numPaidAmout = receipt.numAmount;
                var _Billheaders = db.BillHeaders.Where(b => b.numBookingRefferenceID == receipt.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.numBalanceToPay.Value != 0).OrderBy(b => b.numBillHeaderID).ToList();
                foreach (var _billheader in _Billheaders)
                {
                    if (numPaidAmout != 0)
                    {
                        if (_billheader.numBalanceToPay >= numPaidAmout)
                        {
                            BillHeader billHeader = db.BillHeaders.Find(_billheader.numBillHeaderID);
                            billHeader.numPayedAmount = billHeader.numPayedAmount + numPaidAmout;
                            billHeader.numBalanceToPay = billHeader.numBalanceToPay - numPaidAmout;
                            db.Entry(billHeader).State = EntityState.Modified;
                            db.SaveChanges();

                            numPaidAmout = numPaidAmout - numPaidAmout;
                        }
                        else
                        {
                            BillHeader billHeader = db.BillHeaders.Find(_billheader.numBillHeaderID);
                            billHeader.numPayedAmount = billHeader.numPayedAmount + _billheader.numBalanceToPay;
                            billHeader.numBalanceToPay = billHeader.numBalanceToPay - _billheader.numBalanceToPay;
                            db.Entry(billHeader).State = EntityState.Modified;
                            db.SaveChanges();

                            numPaidAmout = numPaidAmout - _billheader.numPayedAmount;
                        }
                    }
                }
            }

            var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == receipt.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
            if (numBalanceToPayRefference == 0)
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(receipt.numBookingRefferenceID);
                bookingRefference.bitPayed = true;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }

            TempData["ReceiptStatus"] = "Saved";
            return RedirectToAction("Create");
        }

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                _rptRecId = -1;
                _rptStatus = false;
                var Receipts = db.Receipts.Where(r => r.bitActive == true).OrderByDescending(x => x.dtCreatedDate).ToList();
                return View(Receipts);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Receipt receipt = db.Receipts.Find(id);
            if (receipt == null)
            {
                return HttpNotFound();
            }

            receipt.bitActive = false;
            receipt.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            receipt.dtDeletedDate = System.DateTime.Now;

            db.Entry(receipt).State = EntityState.Modified;
            db.SaveChanges();

            TempData["ReceiptStatus"] = "Deleted";
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