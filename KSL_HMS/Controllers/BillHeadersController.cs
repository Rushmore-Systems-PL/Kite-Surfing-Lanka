using KSL_HMS.DB;
using KSL_HMS.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class BillHeadersController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                var billHeaders = db.BillHeaders.Include(b => b.BookingHeader).Include(b => b.BookingRefference).Where(b => b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(x => new BillHeaderDTO
                {
                    numBookingRefferenceID = x.numBookingRefferenceID,
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    numBalanceToPay = 0,
                    numPayedAmount = 0,
                    numTotalCost = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Sum(bh => (decimal?)bh.numTotalCost) ?? 0.00m,
                    dtCreatedDate = x.dtCreatedDate
                }).DistinctBy(b => b.numBookingRefferenceID).OrderByDescending(b => b.dtCreatedDate).ToList();
                foreach (var billHeader in billHeaders)
                {
                    var numPayedAmount = db.Receipts.Where(r => r.numBookingRefferenceID == billHeader.numBookingRefferenceID && r.bitActive == true).Sum(ra => (decimal?)ra.numAmount) ?? 0.00m;
                    billHeader.numPayedAmount = numPayedAmount;
                    billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
                }
                return View(billHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        // Payments must be calculated in real time to catch over payments
        public ActionResult OutstandingBills()
        {
            if (Session["UserID"] != null)
            {
                var billHeaders = db.BillHeaders.Include(b => b.BookingHeader).Include(b => b.BookingRefference).Where(b => b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(x => new BillHeaderDTO
                {
                    numBookingRefferenceID = x.numBookingRefferenceID,
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    numBalanceToPay = 0,
                    numPayedAmount = 0,
                    numTotalCost = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Sum(bh => (decimal?)bh.numTotalCost) ?? 0,
                    dtCreatedDate = x.dtCreatedDate
                }).DistinctBy(b => b.numBookingRefferenceID).OrderByDescending(b => b.dtCreatedDate).ToList();
                List<BillHeaderDTO> outstandingBillHeaders = new List<BillHeaderDTO>();
                foreach (var billHeader in billHeaders) {
                    var numPayedAmount = db.Receipts.Where(r => r.numBookingRefferenceID == billHeader.numBookingRefferenceID && r.bitActive == true).Sum(ra => (decimal?)ra.numAmount) ?? 0;
                    billHeader.numPayedAmount = numPayedAmount;
                    billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
                    if (billHeader.numBalanceToPay > 0) {
                        outstandingBillHeaders.Add(billHeader);
                    }
                }
                return View(outstandingBillHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        // Payments must be calculated in real time to catch overpaymemts 
        public ActionResult Details(int? id)
        {
            if (Session["UserID"] != null)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                BookingRefference bookingRefference = db.BookingRefferences.Find(id);
                if (bookingRefference == null)
                {
                    return HttpNotFound();
                }

                var billHeaders = db.BillHeaders.Include(b => b.BookingHeader).Include(b => b.BookingRefference).Where(b => b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && b.BookingRefference.bitClosed == false).Select(x => new BillHeaderDTO
                {
                    numBookingRefferenceID = x.numBookingRefferenceID,
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    dtCreatedDate = x.BookingRefference.dtCreatedDate,
                    numBalanceToPay = 0,
                    numPayedAmount = 0,
                    numTotalCost = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Sum(bh => (decimal?)bh.numTotalCost) ?? 0,
                }).Distinct().FirstOrDefault();

                var BookingHeaders = db.BookingHeaders.Where(h => h.bitActive == true && h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).OrderByDescending(x => x.numBookingHeaderID).ToList();
                var BookingHeaderIDs = BookingHeaders.Select(h => h.numBookingHeaderID).ToList();
                var BookingDetails = db.BookingDetails.Where(d => d.bitActive == true && BookingHeaderIDs.Contains(d.numBookingHeaderID.Value)).OrderBy(x => x.dtCreatedDate).ToList();

                ViewBag.GuestCount = BookingDetails.Count();
                ViewBag.RoomsCount = BookingHeaders.Count();
                ViewBag.BookingHeaders = BookingHeaders;
                ViewBag.BookingDetails = BookingDetails;
                ViewBag.BillDetails = db.BillDetails.Where(b => b.BillHeader.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && b.BillHeader.BookingRefference.bitActive == true && b.BillHeader.BookingRefference.bitClosed == false && b.BillHeader.bitActive == true && b.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                ViewBag.Receipts = db.Receipts.Where(r => r.bitActive == true && r.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).OrderBy(x => x.dtCreatedDate).ToList();

                var numPayedAmount = db.Receipts.Where(r => r.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && r.bitActive == true).Sum(ra => (decimal?)ra.numAmount) ?? 0;
                billHeaders.numPayedAmount = numPayedAmount;
                billHeaders.numBalanceToPay = billHeaders.numTotalCost - billHeaders.numPayedAmount;

                return View(billHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Invoice(int? id)
        {
            if (Session["UserID"] != null)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                BookingRefference bookingRefference = db.BookingRefferences.Find(id);
                if (bookingRefference == null)
                {
                    return HttpNotFound();
                }

                var billHeaders = db.BillHeaders.Include(b => b.BookingHeader).Include(b => b.BookingRefference).Where(b => b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && b.BookingRefference.bitClosed == false).Select(x => new BillHeaderDTO
                {
                    numBookingRefferenceID = x.numBookingRefferenceID,
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    dtCreatedDate = x.BookingRefference.dtCreatedDate,
                    numBalanceToPay = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numBalanceToPay).Sum(),
                    numPayedAmount = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numPayedAmount).Sum(),
                    numTotalCost = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numTotalCost).Sum(),
                }).Distinct().FirstOrDefault();

                var BookingHeaders = db.BookingHeaders.Where(h => h.bitActive == true && h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).OrderByDescending(x => x.numBookingHeaderID).ToList();
                var BookingHeaderIDs = BookingHeaders.Select(h => h.numBookingHeaderID).ToList();
                var BookingDetails = db.BookingDetails.Where(d => d.bitActive == true && BookingHeaderIDs.Contains(d.numBookingHeaderID.Value)).OrderBy(x => x.dtCreatedDate).ToList();

                ViewBag.GuestCount = BookingDetails.Count();
                ViewBag.RoomsCount = BookingHeaders.Count();
                ViewBag.BookingHeaders = BookingHeaders;
                ViewBag.BookingDetails = BookingDetails;
                ViewBag.BillDetails = db.BillDetails.Where(b => b.BillHeader.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && b.BillHeader.BookingRefference.bitActive == true && b.BillHeader.BookingRefference.bitClosed == false && b.BillHeader.bitActive == true && b.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                ViewBag.Receipts = db.Receipts.Where(r => r.bitActive == true && r.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).OrderBy(x => x.dtCreatedDate).ToList();
                return View(billHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult CloseBill()
        {
            if (Session["UserID"] != null)
            {
                var billHeaders = db.BillHeaders.Include(b => b.BookingHeader).Include(b => b.BookingRefference).Where(b => b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(x => new BillHeaderDTO
                {
                    numBookingRefferenceID = x.numBookingRefferenceID,
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    numBalanceToPay = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numBalanceToPay).Sum(),
                    numPayedAmount = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numPayedAmount).Sum(),
                    numTotalCost = db.BillHeaders.Where(b => b.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true && b.BookingRefference.bitClosed == false).Select(bh => bh.numTotalCost).Sum(),
                    dtCreatedDate = x.dtCreatedDate
                }).DistinctBy(b => b.numBookingRefferenceID).OrderByDescending(b => b.dtCreatedDate).ToList();
                return View(billHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Close(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BookingRefference bookingRefference = db.BookingRefferences.Find(id);
            if (bookingRefference == null)
            {
                return HttpNotFound();
            }

            bookingRefference.bitClosed = true;
            bookingRefference.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            bookingRefference.dtUpdatetDate = System.DateTime.Now;

            db.Entry(bookingRefference).State = EntityState.Modified;
            db.SaveChanges();

            TempData["BillStatus"] = "Deleted";
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