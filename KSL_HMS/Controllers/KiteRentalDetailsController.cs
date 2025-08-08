using KSL_HMS.DB;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class KiteRentalDetailsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public ActionResult Create()
        {
            if (Session["UserID"] != null)
            {
                var Rooms = (from bh in db.BookingHeaders
                             join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                             where bh.bitCheckedIn == true && bh.bitCheckedOut == false && bh.bitActive == true && bf.bitClosed == false
                             select new
                             {
                                 Rid = bh.numBookingHeaderID,
                                 Rdesc = bf.bitExternalBooking == false ? "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo : "REF : " + bf.varBookingRefferenceNo,
                                 Rorder = bh.dtCreatedDate
                             }).OrderByDescending(x => x.Rorder).ToList();
                ViewBag.numBookingHeaderID = new SelectList(Rooms, "Rid", "Rdesc");
                var _RentalItems = db.KiteRentalRates.Where(r => r.bitActive == true).Select(r => r.Item.numItemTypeID).Distinct().ToList();
                var Items = (from it in db.Items
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where it.bitActive == true && it.bitIsDeleted == false && lit.bitActive == true && _RentalItems.Contains(lit.numItemTypeID)
                             select new
                             {
                                 ITid = it.numItemID,
                                 ITdesc = it.varItemName,
                                 ITordr = it.numItemOrder
                             }).OrderBy(x => x.ITordr).ToList();
                ViewBag.numItemID = new SelectList(Items, "ITid", "ITdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult LoadGuests(int? id)
        {
            return Json(db.BookingDetails.Where(b => b.bitActive == true && b.numBookingHeaderID == id).OrderBy(x => x.dtCreatedDate).Select(x => new { id = x.numBookingDetailID, name = x.Guest.varGuestName }), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(KiteRentalDetail kiteRentalDetail)
        {
            var numBookingRefferenceID = db.BookingHeaders.Where(h => h.numBookingHeaderID == kiteRentalDetail.numBookingHeaderID).Select(h => h.numBookingRefferenceID).First();
            var kiteRentalHeader = db.KiteRentalHeaders.Where(kh => kh.numBookingRefferenceID == numBookingRefferenceID && kh.numItemID == kiteRentalDetail.numItemID && kh.bitActive == true).FirstOrDefault();

            if (kiteRentalHeader == null)
            {
                decimal? numCalCulatedRate = this.LoadItemRate(kiteRentalDetail.numItemID.Value, kiteRentalDetail.numKiteRentalDays.Value);
                decimal? total = (numCalCulatedRate * kiteRentalDetail.numKiteRentalQuantity) * kiteRentalDetail.numKiteRentalDays;

                KiteRentalHeader newkiteRentalHeader = new KiteRentalHeader();
                newkiteRentalHeader.numBookingRefferenceID = numBookingRefferenceID;
                newkiteRentalHeader.numItemID = kiteRentalDetail.numItemID;
                newkiteRentalHeader.numTotalRate = numCalCulatedRate;
                newkiteRentalHeader.numTotalDays = kiteRentalDetail.numKiteRentalDays;
                newkiteRentalHeader.numTotalCost = decimal.Round(total.Value, 2, MidpointRounding.AwayFromZero);
                newkiteRentalHeader.bitActive = true;
                newkiteRentalHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                newkiteRentalHeader.dtCreatedDate = System.DateTime.Now;
                db.KiteRentalHeaders.Add(newkiteRentalHeader);
                db.SaveChanges();

                kiteRentalDetail.numKiteRentalHeaderID = newkiteRentalHeader.numKiteRentalHeaderID;
                kiteRentalDetail.numKiteRentalRate = newkiteRentalHeader.numTotalRate;
                kiteRentalDetail.numKiteRentalCost = newkiteRentalHeader.numTotalCost;
                kiteRentalDetail.bitActive = true;
                kiteRentalDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                kiteRentalDetail.dtCreatedDate = System.DateTime.Now;
                db.KiteRentalDetails.Add(kiteRentalDetail);
                db.SaveChanges();

                BookingHeader bookingHeader = db.BookingHeaders.Find(kiteRentalDetail.numBookingHeaderID);
                var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == kiteRentalDetail.numBookingHeaderID && b.numBookingRefferenceID == numBookingRefferenceID).Select(b => b.numBillHeaderID).FirstOrDefault();

                BillDetail billDetail = new BillDetail();
                billDetail.numBillHeaderID = numBillHeaderID;
                billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Rental").Select(c => c.numChargeTypeID).FirstOrDefault();
                billDetail.numRoomID = bookingHeader.numRoomID;
                billDetail.numLocationID = db.Locations.Where(c => c.bitActive == true && c.varLocationName == "Rental").Select(c => c.numLocationID).FirstOrDefault();
                billDetail.numItemID = kiteRentalDetail.numItemID;
                billDetail.numBookingDetailID = kiteRentalDetail.numBookingDetailID;
                billDetail.numKiteRentalDetailID = kiteRentalDetail.numKiteRentalDetailID;
                billDetail.numCost = numCalCulatedRate;
                billDetail.numDuration = kiteRentalDetail.numKiteRentalDays;
                billDetail.numQuantity = kiteRentalDetail.numKiteRentalQuantity;
                billDetail.bitDiscountOffered = false;
                billDetail.numFinalCost = kiteRentalDetail.numKiteRentalCost;
                billDetail.bitActive = true;
                billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
                billDetail.dtCreatedDate = System.DateTime.Now;
                db.BillDetails.Add(billDetail);
                db.SaveChanges();

                BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
                billHeader.numTotalCost = billHeader.numTotalCost + billDetail.numFinalCost;
                if (billHeader.numPayedAmount == 0)
                {
                    billHeader.numBalanceToPay = billHeader.numBalanceToPay + billDetail.numFinalCost;
                }
                else
                {
                    billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
                }
                billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
                billHeader.dtUpdatetDate = System.DateTime.Now;
                db.Entry(billHeader).State = EntityState.Modified;
                db.SaveChanges();

                var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
                if (numBalanceToPayRefference == 0)
                {
                    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                    bookingRefference.bitPayed = true;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                    bookingRefference.bitPayed = false;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            else
            {
                decimal? numTotalDays = 0;
                decimal? numTotalCost = 0;
                decimal? numFinalCost = 0;
                decimal? numCalCulatedRate = this.LoadItemRate(kiteRentalDetail.numItemID.Value, kiteRentalHeader.numTotalDays.Value + kiteRentalDetail.numKiteRentalDays.Value);

                var KiteRentalDetails = db.KiteRentalDetails.Where(kd => kd.bitActive == true && kd.numKiteRentalHeaderID == kiteRentalHeader.numKiteRentalHeaderID).ToList();
                if (KiteRentalDetails != null)
                {
                    foreach (var _kiteRentalDetail in KiteRentalDetails)
                    {
                        decimal? numKiteRentingCost = (numCalCulatedRate * _kiteRentalDetail.numKiteRentalQuantity) * _kiteRentalDetail.numKiteRentalDays;

                        _kiteRentalDetail.numItemID = kiteRentalDetail.numItemID;
                        _kiteRentalDetail.numKiteRentalRate = numCalCulatedRate;
                        _kiteRentalDetail.numKiteRentalCost = decimal.Round(numKiteRentingCost.Value, 2, MidpointRounding.AwayFromZero);
                        _kiteRentalDetail.bitActive = true;
                        _kiteRentalDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        _kiteRentalDetail.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(_kiteRentalDetail).State = EntityState.Modified;
                        db.SaveChanges();

                        numTotalDays = numTotalDays + _kiteRentalDetail.numKiteRentalDays;
                        numTotalCost = numTotalCost + _kiteRentalDetail.numKiteRentalCost;
                    }
                }

                kiteRentalDetail.numKiteRentalHeaderID = kiteRentalHeader.numKiteRentalHeaderID;
                kiteRentalDetail.numKiteRentalRate = numCalCulatedRate;
                kiteRentalDetail.numKiteRentalCost = (numCalCulatedRate * kiteRentalDetail.numKiteRentalQuantity) * kiteRentalDetail.numKiteRentalDays;
                kiteRentalDetail.bitActive = true;
                kiteRentalDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                kiteRentalDetail.dtCreatedDate = System.DateTime.Now;
                db.KiteRentalDetails.Add(kiteRentalDetail);
                db.SaveChanges();

                kiteRentalHeader.numTotalRate = numCalCulatedRate;
                kiteRentalHeader.numTotalDays = numTotalDays + kiteRentalDetail.numKiteRentalDays;
                kiteRentalHeader.numTotalCost = numTotalCost + kiteRentalDetail.numKiteRentalCost;
                kiteRentalHeader.bitActive = true;
                kiteRentalHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                kiteRentalHeader.dtUpdatetDate = System.DateTime.Now;
                db.Entry(kiteRentalHeader).State = EntityState.Modified;
                db.SaveChanges();

                BookingHeader bookingHeader = db.BookingHeaders.Find(kiteRentalDetail.numBookingHeaderID);
                var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == kiteRentalDetail.numBookingHeaderID && b.numBookingRefferenceID == numBookingRefferenceID).Select(b => b.numBillHeaderID).FirstOrDefault();

                List<int> _billDetailIDs = new List<int>();
                var BillDetails = db.BillDetails.Where(bd => bd.numBillHeaderID == numBillHeaderID && bd.numItemID == kiteRentalDetail.numItemID && bd.bitActive == true).ToList();
                if (BillDetails != null)
                {
                    foreach (var _billDetail in BillDetails)
                    {
                        decimal? numFinalRate = (numCalCulatedRate * _billDetail.numQuantity) * _billDetail.numDuration;

                        _billDetail.numCost = numCalCulatedRate;
                        _billDetail.numFinalCost = decimal.Round(numFinalRate.Value, 2, MidpointRounding.AwayFromZero);
                        _billDetail.bitActive = true;
                        _billDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        _billDetail.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(_billDetail).State = EntityState.Modified;
                        db.SaveChanges();

                        _billDetailIDs.Add(_billDetail.numBillDetailID);
                        numFinalCost = numFinalCost + _billDetail.numFinalCost;
                    }
                }

                BillDetail billDetail = new BillDetail();
                billDetail.numBillHeaderID = numBillHeaderID;
                billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Rental").Select(c => c.numChargeTypeID).FirstOrDefault();
                billDetail.numRoomID = bookingHeader.numRoomID;
                billDetail.numLocationID = db.Locations.Where(c => c.bitActive == true && c.varLocationName == "Rental").Select(c => c.numLocationID).FirstOrDefault();
                billDetail.numItemID = kiteRentalDetail.numItemID;
                billDetail.numBookingDetailID = kiteRentalDetail.numBookingDetailID;
                billDetail.numKiteRentalDetailID = kiteRentalDetail.numKiteRentalDetailID;
                billDetail.numCost = numCalCulatedRate;
                billDetail.numDuration = kiteRentalDetail.numKiteRentalDays;
                billDetail.numQuantity = kiteRentalDetail.numKiteRentalQuantity;
                billDetail.bitDiscountOffered = false;
                billDetail.numFinalCost = kiteRentalDetail.numKiteRentalCost;
                billDetail.bitActive = true;
                billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]); ;
                billDetail.dtCreatedDate = System.DateTime.Now;
                db.BillDetails.Add(billDetail);
                db.SaveChanges();

                _billDetailIDs.Add(billDetail.numBillDetailID);
                var numBillFinalCost = db.BillDetails.Where(bd => bd.numBillHeaderID == numBillHeaderID && bd.bitActive == true && !_billDetailIDs.Contains(bd.numBillDetailID)).Select(bd => bd.numFinalCost).Sum();

                BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
                billHeader.numTotalCost = numBillFinalCost + numFinalCost + billDetail.numFinalCost;
                if (billHeader.numPayedAmount == 0)
                {
                    billHeader.numBalanceToPay = billHeader.numTotalCost;
                }
                else
                {
                    billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
                }
                billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                billHeader.dtUpdatetDate = System.DateTime.Now;
                db.Entry(billHeader).State = EntityState.Modified;
                db.SaveChanges();

                var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
                if (numBalanceToPayRefference == 0)
                {
                    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                    bookingRefference.bitPayed = true;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                    bookingRefference.bitPayed = false;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            TempData["kiteRentingStatus"] = "Saved";
            return RedirectToAction("Create");
        }

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                var kiteRentalDetails = db.KiteRentalDetails.Include(k => k.BookingDetail).Include(k => k.KiteRentalHeader).Where(b => b.KiteRentalHeader.BookingRefference.bitClosed == false && b.bitActive == true);
                return View(kiteRentalDetails.OrderByDescending(x => x.dtCreatedDate).ToList());
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public decimal? LoadItemRate(int id, decimal dts)
        {
            var KiteRentalRate = db.KiteRentalRates.Where(l => l.bitActive == true && l.numItemID == id && l.numKiteRentalRateDays == dts).FirstOrDefault();
            decimal? numRate;
            if (KiteRentalRate != null)
            {
                numRate = KiteRentalRate.numKiteRentalRate / KiteRentalRate.numKiteRentalRateDays;
            }
            else
            {
                //var lessThan11 = l.TakeWhile(p => p < 11).Last();
                //var greaterThan13 = l.SkipWhile(p => p <= 13).First();

                var _daylist = db.KiteRentalRates.Where(kr => kr.bitActive == true && kr.numItemID == id).Select(kr => kr.numKiteRentalRateDays).OrderBy(kr => kr).ToList();
                decimal _closestDays = 0;
                decimal? numRentalRate = 0;

                if (dts == Convert.ToDecimal(0.5))
                {
                    _closestDays = 1;
                    var halfDayRate = db.KiteRentalRates.Where(l => l.bitActive == true && l.numItemID == id && l.numKiteRentalRateDays == _closestDays).Select(x => x.numKiteRentalRate).FirstOrDefault();
                    numRentalRate = halfDayRate / 2;
                }
                else
                {
                    _closestDays = _daylist.TakeWhile(p => p < dts).Last();
                    numRentalRate = db.KiteRentalRates.Where(l => l.bitActive == true && l.numItemID == id && l.numKiteRentalRateDays == _closestDays).Select(x => x.numKiteRentalRate).FirstOrDefault();
                }

                numRate = numRentalRate / _closestDays;
            }
            return numRate;
            //return decimal.Round(numRate.Value, 2, MidpointRounding.AwayFromZero);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KiteRentalDetail kiteRentalDetail = db.KiteRentalDetails.Find(id);
            if (kiteRentalDetail == null)
            {
                return HttpNotFound();
            }

            decimal? numTotalDays = 0;
            decimal? numTotalCost = 0;
            decimal? numFinalCost = 0;

            var kiteRentalHeader = db.KiteRentalHeaders.Where(kh => kh.numBookingRefferenceID == kiteRentalDetail.KiteRentalHeader.numBookingRefferenceID && kh.numItemID == kiteRentalDetail.KiteRentalHeader.numItemID && kh.bitActive == true).FirstOrDefault();
            decimal? numCalCulatedRate = this.LoadItemRate(kiteRentalDetail.KiteRentalHeader.numItemID.Value, kiteRentalHeader.numTotalDays.Value - kiteRentalDetail.numKiteRentalDays.Value);

            var KiteRentalDetails = db.KiteRentalDetails.Where(kd => kd.bitActive == true && kd.numKiteRentalHeaderID == kiteRentalHeader.numKiteRentalHeaderID).ToList();
            if (KiteRentalDetails != null)
            {
                foreach (var _kiteRentalDetail in KiteRentalDetails)
                {
                    if (_kiteRentalDetail.numKiteRentalDetailID == kiteRentalDetail.numKiteRentalDetailID)
                    {
                        kiteRentalDetail.numItemID = kiteRentalDetail.KiteRentalHeader.numItemID;
                        kiteRentalDetail.bitActive = false;
                        kiteRentalDetail.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
                        kiteRentalDetail.dtDeletedDate = System.DateTime.Now;
                        db.Entry(kiteRentalDetail).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        decimal? numKiteRentingCost = (numCalCulatedRate * _kiteRentalDetail.numKiteRentalQuantity) * _kiteRentalDetail.numKiteRentalDays;

                        _kiteRentalDetail.numItemID = kiteRentalDetail.KiteRentalHeader.numItemID;
                        _kiteRentalDetail.numKiteRentalRate = numCalCulatedRate;
                        _kiteRentalDetail.numKiteRentalCost = decimal.Round(numKiteRentingCost.Value, 2, MidpointRounding.AwayFromZero);
                        _kiteRentalDetail.bitActive = true;
                        _kiteRentalDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        _kiteRentalDetail.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(_kiteRentalDetail).State = EntityState.Modified;
                        db.SaveChanges();

                        numTotalDays = numTotalDays + _kiteRentalDetail.numKiteRentalDays;
                        numTotalCost = numTotalCost + _kiteRentalDetail.numKiteRentalCost;
                    }
                }
            }

            kiteRentalHeader.numTotalRate = numCalCulatedRate;
            kiteRentalHeader.numTotalDays = numTotalDays;
            kiteRentalHeader.numTotalCost = numTotalCost;
            kiteRentalHeader.bitActive = true;
            kiteRentalHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
            kiteRentalHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(kiteRentalHeader).State = EntityState.Modified;
            db.SaveChanges();

            var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == kiteRentalDetail.BookingDetail.numBookingHeaderID && b.numBookingRefferenceID == kiteRentalDetail.KiteRentalHeader.numBookingRefferenceID).Select(b => b.numBillHeaderID).FirstOrDefault();

            List<int> _billDetailIDs = new List<int>();
            var BillDetails = db.BillDetails.Where(bd => bd.numBillHeaderID == numBillHeaderID && bd.numItemID == kiteRentalDetail.KiteRentalHeader.numItemID && bd.bitActive == true).ToList();
            if (BillDetails != null)
            {
                foreach (var _billDetail in BillDetails)
                {
                    if (_billDetail.numKiteRentalDetailID == kiteRentalDetail.numKiteRentalDetailID)
                    {
                        _billDetail.bitActive = false;
                        _billDetail.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
                        _billDetail.dtDeletedDate = System.DateTime.Now;
                        db.Entry(_billDetail).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        decimal? numFinalRate = (numCalCulatedRate * _billDetail.numQuantity) * _billDetail.numDuration;

                        _billDetail.numCost = numCalCulatedRate;
                        _billDetail.numFinalCost = decimal.Round(numFinalRate.Value, 2, MidpointRounding.AwayFromZero);
                        _billDetail.bitActive = true;
                        _billDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        _billDetail.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(_billDetail).State = EntityState.Modified;
                        db.SaveChanges();

                        _billDetailIDs.Add(_billDetail.numBillDetailID);
                        numFinalCost = numFinalCost + _billDetail.numFinalCost;
                    }
                }
            }

            var numBillFinalCost = db.BillDetails.Where(bd => bd.numBillHeaderID == numBillHeaderID && bd.bitActive == true && !_billDetailIDs.Contains(bd.numBillDetailID)).Select(bd => bd.numFinalCost).Sum();

            BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
            billHeader.numTotalCost = numBillFinalCost + numFinalCost;
            if (billHeader.numPayedAmount == 0)
            {
                billHeader.numBalanceToPay = billHeader.numTotalCost;
            }
            else
            {
                billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
            }
            billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
            billHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
            if (numBalanceToPayRefference == 0)
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = true;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = false;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }

            TempData["kiteRentingStatus"] = "Deleted";
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