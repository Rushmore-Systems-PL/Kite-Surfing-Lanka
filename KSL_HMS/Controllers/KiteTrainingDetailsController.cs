using KSL_HMS.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class KiteTrainingDetailsController : Controller
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

                ViewBag.numInstructorID = new SelectList(db.Instructors.Where(i => i.bitActive == true).OrderBy(x => x.varInstructorName), "numInstructorID", "varInstructorName");
                var _TrainingItemTypes = db.KiteTrainingRates.Where(r => r.bitActive == true).Select(r => r.Item.numItemTypeID).Distinct().ToList();
                var Items = (from it in db.Items
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where it.bitActive == true && it.bitIsDeleted == false && lit.bitActive == true && _TrainingItemTypes.Contains(lit.numItemTypeID)
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
        public ActionResult Create(KiteTrainingDetail kiteTrainingDetail)
        {
            var numBookingRefferenceID = db.BookingHeaders.Where(h => h.numBookingHeaderID == kiteTrainingDetail.numBookingHeaderID).Select(h => h.numBookingRefferenceID).First();
            var kiteTrainingHeader = db.KiteTrainingHeaders.Where(kh => kh.numBookingRefferenceID == numBookingRefferenceID && kh.numItemID == kiteTrainingDetail.numItemID && kh.bitActive == true).FirstOrDefault();

            if (kiteTrainingHeader == null)
            {
                decimal? numCalCulatedRate = this.LoadItemRate(kiteTrainingDetail.numItemID.Value, kiteTrainingDetail.numKiteTrainingHours.Value);
                decimal total = (numCalCulatedRate.Value * kiteTrainingDetail.numTrainingPaxCount.Value) * kiteTrainingDetail.numKiteTrainingHours.Value;

                KiteTrainingHeader newkiteTrainingHeader = new KiteTrainingHeader();
                newkiteTrainingHeader.numBookingRefferenceID = numBookingRefferenceID;
                newkiteTrainingHeader.numItemID = kiteTrainingDetail.numItemID;
                newkiteTrainingHeader.numTotalRate = numCalCulatedRate;
                newkiteTrainingHeader.numTotalHours = kiteTrainingDetail.numKiteTrainingHours;
                newkiteTrainingHeader.numTotalCost = decimal.Round(total, 2, MidpointRounding.ToEven);
                newkiteTrainingHeader.bitActive = true;
                newkiteTrainingHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                newkiteTrainingHeader.dtCreatedDate = System.DateTime.Now;
                db.KiteTrainingHeaders.Add(newkiteTrainingHeader);
                db.SaveChanges();

                kiteTrainingDetail.numKiteTrainingHeaderID = newkiteTrainingHeader.numKiteTrainingHeaderID;
                kiteTrainingDetail.numKiteTrainingRate = newkiteTrainingHeader.numTotalRate;
                kiteTrainingDetail.numKiteTrainingCost = newkiteTrainingHeader.numTotalCost;
                kiteTrainingDetail.bitActive = true;
                kiteTrainingDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                kiteTrainingDetail.dtCreatedDate = System.DateTime.Now;
                db.KiteTrainingDetails.Add(kiteTrainingDetail);
                db.SaveChanges();

                BookingHeader bookingHeader = db.BookingHeaders.Find(kiteTrainingDetail.numBookingHeaderID);
                var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == kiteTrainingDetail.numBookingHeaderID && b.numBookingRefferenceID == numBookingRefferenceID).Select(b => b.numBillHeaderID).FirstOrDefault();

                BillDetail billDetail = new BillDetail();
                billDetail.numBillHeaderID = numBillHeaderID;
                billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Training").Select(c => c.numChargeTypeID).FirstOrDefault();
                billDetail.numRoomID = bookingHeader.numRoomID;
                billDetail.numLocationID = db.Locations.Where(c => c.bitActive == true && c.varLocationName == "Kite School").Select(c => c.numLocationID).FirstOrDefault();
                billDetail.numItemID = kiteTrainingDetail.numItemID;
                billDetail.numBookingDetailID = kiteTrainingDetail.numBookingDetailID;
                billDetail.numKiteTrainingDetailID = kiteTrainingDetail.numKiteTrainingDetailID;
                billDetail.numCost = numCalCulatedRate;
                billDetail.numDuration = kiteTrainingDetail.numKiteTrainingHours;
                billDetail.numQuantity = kiteTrainingDetail.numTrainingPaxCount;
                billDetail.bitDiscountOffered = false;
                billDetail.numFinalCost = kiteTrainingDetail.numKiteTrainingCost;
                billDetail.bitActive = true;
                billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
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
            else
            {
                decimal? numTotalHours = 0;
                decimal? numTotalCost = 0;
                decimal? numFinalCost = 0;
                decimal? numCalCulatedRate = this.LoadItemRate(kiteTrainingDetail.numItemID.Value, kiteTrainingHeader.numTotalHours.Value + kiteTrainingDetail.numKiteTrainingHours.Value);

                var KiteTrainingDetails = db.KiteTrainingDetails.Where(kd => kd.bitActive == true && kd.numKiteTrainingHeaderID == kiteTrainingHeader.numKiteTrainingHeaderID).ToList();
                if (KiteTrainingDetails != null)
                {
                    foreach (var _kiteTrainingDetail in KiteTrainingDetails)
                    {
                        decimal? numKiteTrainingCost = (numCalCulatedRate * _kiteTrainingDetail.numTrainingPaxCount) * _kiteTrainingDetail.numKiteTrainingHours;

                        _kiteTrainingDetail.numItemID = kiteTrainingDetail.numItemID;
                        _kiteTrainingDetail.numKiteTrainingRate = numCalCulatedRate;
                        _kiteTrainingDetail.numKiteTrainingCost = decimal.Round(numKiteTrainingCost.Value, 2, MidpointRounding.AwayFromZero);
                        _kiteTrainingDetail.bitActive = true;
                        _kiteTrainingDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        _kiteTrainingDetail.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(_kiteTrainingDetail).State = EntityState.Modified;
                        db.SaveChanges();

                        numTotalHours = numTotalHours + _kiteTrainingDetail.numKiteTrainingHours;
                        numTotalCost = numTotalCost + _kiteTrainingDetail.numKiteTrainingCost;
                    }
                }

                kiteTrainingDetail.numKiteTrainingHeaderID = kiteTrainingHeader.numKiteTrainingHeaderID;
                kiteTrainingDetail.numKiteTrainingRate = numCalCulatedRate;
                kiteTrainingDetail.numKiteTrainingCost = (numCalCulatedRate * kiteTrainingDetail.numTrainingPaxCount) * kiteTrainingDetail.numKiteTrainingHours;
                kiteTrainingDetail.bitActive = true;
                kiteTrainingDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                kiteTrainingDetail.dtCreatedDate = System.DateTime.Now;
                db.KiteTrainingDetails.Add(kiteTrainingDetail);
                db.SaveChanges();

                kiteTrainingHeader.numTotalRate = numCalCulatedRate;
                kiteTrainingHeader.numTotalHours = numTotalHours + kiteTrainingDetail.numKiteTrainingHours;
                kiteTrainingHeader.numTotalCost = numTotalCost + kiteTrainingDetail.numKiteTrainingCost;
                kiteTrainingHeader.bitActive = true;
                kiteTrainingHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                kiteTrainingHeader.dtUpdatetDate = System.DateTime.Now;
                db.Entry(kiteTrainingHeader).State = EntityState.Modified;
                db.SaveChanges();

                BookingHeader bookingHeader = db.BookingHeaders.Find(kiteTrainingDetail.numBookingHeaderID);
                var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == kiteTrainingDetail.numBookingHeaderID && b.numBookingRefferenceID == numBookingRefferenceID).Select(b => b.numBillHeaderID).FirstOrDefault();

                List<int> _billDetailIDs = new List<int>();
                var BillDetails = db.BillDetails.Where(bd => bd.numBillHeaderID == numBillHeaderID && bd.numItemID == kiteTrainingDetail.numItemID && bd.bitActive == true).ToList();
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
                billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Training").Select(c => c.numChargeTypeID).FirstOrDefault();
                billDetail.numRoomID = bookingHeader.numRoomID;
                billDetail.numLocationID = db.Locations.Where(c => c.bitActive == true && c.varLocationName == "Kite School").Select(c => c.numLocationID).FirstOrDefault();
                billDetail.numItemID = kiteTrainingDetail.numItemID;
                billDetail.numBookingDetailID = kiteTrainingDetail.numBookingDetailID;
                billDetail.numKiteTrainingDetailID = kiteTrainingDetail.numKiteTrainingDetailID;
                billDetail.numCost = numCalCulatedRate;
                billDetail.numDuration = kiteTrainingDetail.numKiteTrainingHours;
                billDetail.numQuantity = kiteTrainingDetail.numTrainingPaxCount;
                billDetail.bitDiscountOffered = false;
                billDetail.numFinalCost = kiteTrainingDetail.numKiteTrainingCost;
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
            TempData["KiteTrainingStatus"] = "Saved";
            return RedirectToAction("Create");
        }

        public decimal? LoadItemRate(int id, decimal hrs)
        {
            var KiteTrainingRate = db.KiteTrainingRates.Where(l => l.bitActive == true && l.numItemID == id && l.numKiteTrainingRateHours == hrs).FirstOrDefault();
            decimal? numRate;
            if (KiteTrainingRate != null)
            {
                numRate = KiteTrainingRate.numKiteTrainingRate / KiteTrainingRate.numKiteTrainingRateHours;
            }
            else
            {
                //var lessThan11 = l.TakeWhile(p => p < 11).Last();
                //var greaterThan13 = l.SkipWhile(p => p <= 13).First();

                var _hourslist = db.KiteTrainingRates.Where(kr => kr.bitActive == true && kr.numItemID == id).Select(kr => kr.numKiteTrainingRateHours).OrderBy(kr => kr).ToList();
                var _closestHours = _hourslist.TakeWhile(p => p < hrs).Last();
                var numTrainingRate = db.KiteTrainingRates.Where(l => l.bitActive == true && l.numItemID == id && l.numKiteTrainingRateHours == _closestHours).Select(x => x.numKiteTrainingRate).FirstOrDefault();
                numRate = numTrainingRate / _closestHours;
            }
            return numRate;
            //return decimal.Round(numRate.Value, 2, MidpointRounding.AwayFromZero);
        }

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                var kiteTrainingDetails = db.KiteTrainingDetails.Include(k => k.Instructor).Include(k => k.BookingDetail).Include(k => k.KiteTrainingHeader).Where(b => b.KiteTrainingHeader.BookingRefference.bitClosed == false && b.bitActive == true);
                return View(kiteTrainingDetails.OrderByDescending(x => x.dtCreatedDate).ToList());
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
            KiteTrainingDetail kiteTrainingDetail = db.KiteTrainingDetails.Find(id);
            if (kiteTrainingDetail == null)
            {
                return HttpNotFound();
            }

            decimal? numTotalHours = 0;
            decimal? numTotalCost = 0;
            decimal? numFinalCost = 0;

            var kiteTrainingHeader = db.KiteTrainingHeaders.Where(kh => kh.numBookingRefferenceID == kiteTrainingDetail.KiteTrainingHeader.numBookingRefferenceID && kh.numItemID == kiteTrainingDetail.KiteTrainingHeader.numItemID && kh.bitActive == true).FirstOrDefault();
            decimal? numCalCulatedRate = this.LoadItemRate(kiteTrainingDetail.KiteTrainingHeader.numItemID.Value, kiteTrainingHeader.numTotalHours.Value - kiteTrainingDetail.numKiteTrainingHours.Value);

            var KiteTrainingDetails = db.KiteTrainingDetails.Where(kd => kd.bitActive == true && kd.numKiteTrainingHeaderID == kiteTrainingHeader.numKiteTrainingHeaderID).ToList();
            if (KiteTrainingDetails != null)
            {
                foreach (var _kiteTrainingDetail in KiteTrainingDetails)
                {
                    if (_kiteTrainingDetail.numKiteTrainingDetailID == kiteTrainingDetail.numKiteTrainingDetailID)
                    {
                        kiteTrainingDetail.numItemID = kiteTrainingDetail.KiteTrainingHeader.numItemID;
                        kiteTrainingDetail.bitActive = false;
                        kiteTrainingDetail.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
                        kiteTrainingDetail.dtDeletedDate = System.DateTime.Now;
                        db.Entry(kiteTrainingDetail).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    else
                    {
                        decimal? numKiteTrainingCost = (numCalCulatedRate * _kiteTrainingDetail.numTrainingPaxCount) * _kiteTrainingDetail.numKiteTrainingHours;

                        _kiteTrainingDetail.numItemID = kiteTrainingDetail.KiteTrainingHeader.numItemID;
                        _kiteTrainingDetail.numKiteTrainingRate = numCalCulatedRate;
                        _kiteTrainingDetail.numKiteTrainingCost = decimal.Round(numKiteTrainingCost.Value, 2, MidpointRounding.AwayFromZero);
                        _kiteTrainingDetail.bitActive = true;
                        _kiteTrainingDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        _kiteTrainingDetail.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(_kiteTrainingDetail).State = EntityState.Modified;
                        db.SaveChanges();

                        numTotalHours = numTotalHours + _kiteTrainingDetail.numKiteTrainingHours;
                        numTotalCost = numTotalCost + _kiteTrainingDetail.numKiteTrainingCost;
                    }
                }
            }

            kiteTrainingHeader.numTotalRate = numCalCulatedRate;
            kiteTrainingHeader.numTotalHours = numTotalHours;
            kiteTrainingHeader.numTotalCost = numTotalCost;
            kiteTrainingHeader.bitActive = true;
            kiteTrainingHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
            kiteTrainingHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(kiteTrainingHeader).State = EntityState.Modified;
            db.SaveChanges();

            var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == kiteTrainingDetail.BookingDetail.numBookingHeaderID && b.numBookingRefferenceID == kiteTrainingDetail.KiteTrainingHeader.numBookingRefferenceID).Select(b => b.numBillHeaderID).FirstOrDefault();

            List<int> _billDetailIDs = new List<int>();
            var BillDetails = db.BillDetails.Where(bd => bd.numBillHeaderID == numBillHeaderID && bd.numItemID == kiteTrainingDetail.KiteTrainingHeader.numItemID && bd.bitActive == true).ToList();
            if (BillDetails != null)
            {
                foreach (var _billDetail in BillDetails)
                {
                    if (_billDetail.numKiteTrainingDetailID == kiteTrainingDetail.numKiteTrainingDetailID)
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
                        _billDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
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

            TempData["KiteTrainingStatus"] = "Deleted";
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