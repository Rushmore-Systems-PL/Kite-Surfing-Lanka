using KSL_HMS.DB;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class GuestsController : Controller
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
                ViewBag.Guests = db.Guests.Where(i => i.bitActive == true).OrderByDescending(x => x.dtCreatedDate).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Create(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BookingHeader bookingHeader = db.BookingHeaders.Find(id);
            if (bookingHeader == null)
            {
                return HttpNotFound();
            }
            if (bookingHeader.bitKSLessonRequired != false)
            {
                bookingHeader.varKsLessonRequiredStartToEndDates = bookingHeader.dtKsLessonRequiredFrom.Value.ToString("yyyy-MM-dd") + " to " + bookingHeader.dtKsLessonRequiredTo.Value.ToString("yyyy-MM-dd");
            }
            if (bookingHeader.bitKSRentalRequired != false)
            {
                bookingHeader.varKSRentalRequiredStartToEndDates = bookingHeader.dtKSRentalRequiredFrom.Value.ToString("yyyy-MM-dd") + " to " + bookingHeader.dtKSRentalRequiredTo.Value.ToString("yyyy-MM-dd");
            }
            ViewBag.GuestChargeType = new SelectList(db.GuestChargeTypes.Where(g => g.bitActive == true).OrderBy(x => x.varGuestChargeTypeName).ToList(), "numGuestChargeTypeID", "varGuestChargeTypeName");
            return PartialView("Create", bookingHeader);
        }

        public JsonResult GetList(string name)
        {
            var list = db.Guests.Where(x => x.varGuestPPNo.Contains(name) || x.varGuestEmail.Contains(name) && x.bitActive == true).Select(x => new
            {
                id = x.numGuestID,
                label = "Passport No: " + x.varGuestPPNo + " | Email: " + x.varGuestEmail,
                name = x.varGuestName,
                ppno = x.varGuestPPNo,
                email = x.varGuestEmail,
                age = x.numGuestChargeTypeID,
                order = x.dtCreatedDate
            }).OrderByDescending(x => x.order).ToList();
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection formCollection)
        {
            var StandardRateBit = formCollection.GetValues("bitStandardRateValue");
            var Rate = formCollection.GetValues("numRate");
            var LessonRequiredBit = formCollection.GetValues("bitKSLessonRequiredValue");
            var LessonRequiredsFrom = formCollection.GetValues("dtKsLessonRequiredFrom");
            var LessonRequiredsTo = formCollection.GetValues("dtKsLessonRequiredTo");
            var RentalRequiredBit = formCollection.GetValues("bitKSRentalRequiredValue");
            var RentalRequiredsFrom = formCollection.GetValues("dtKSRentalRequiredFrom");
            var RentalRequiredsTo = formCollection.GetValues("dtKSRentalRequiredTo");
            var AgentRequiredBits = formCollection.GetValues("bitKSAgentRequiredValue");
            var AgentNames = formCollection.GetValues("varAgentName");
            var BookingRefferenceID = formCollection.GetValues("numBookingRefferenceID");
            var numRefferenceID = Convert.ToInt32(BookingRefferenceID[0]);
            var BookingHeaderID = formCollection.GetValues("numBookingHeaderID");
            var numBookingID = Convert.ToInt32(BookingHeaderID[0]);
            var GuestPPNo = formCollection.GetValues("varGuestPPNo");
            var GuestName = formCollection.GetValues("varGuestName");
            var GuestEmail = formCollection.GetValues("varGuestEmail");
            var PrimaryGuest = formCollection.GetValues("PrimaryGuest");
            var GuestChargeType = formCollection.GetValues("numGuestChargeTypeID");
            var CheckInDateTime = formCollection.GetValues("dtCheckInDateTime");
            var dtCheckInDateTime = Convert.ToDateTime(CheckInDateTime[0]);

            BookingHeader bookingHeader = db.BookingHeaders.Find(numBookingID);

            if (bookingHeader.bitCheckedIn != true)
            {
                for (int i = 0; i < GuestPPNo.Length; i++)
                {
                    var GuestPP = GuestPPNo[i];
                    var GuestId = db.Guests.Where(g => g.bitActive == true && g.varGuestPPNo.ToUpper() == GuestPP.ToUpper()).Select(g => g.numGuestID).FirstOrDefault();
                    if (GuestId != 0)
                    {
                        Guest exguest = db.Guests.Find(GuestId);
                        exguest.varGuestName = GuestName[i];
                        exguest.varGuestEmail = GuestEmail[i] != "" ? GuestEmail[i] : null;
                        exguest.numGuestChargeTypeID = Convert.ToInt32(GuestChargeType[i]);
                        exguest.bitActive = true;
                        exguest.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        exguest.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(exguest).State = EntityState.Modified;
                        db.SaveChanges();

                        BookingDetail bookingDetail = new BookingDetail();
                        bookingDetail.numBookingHeaderID = numBookingID;
                        bookingDetail.numGuestID = GuestId;
                        bookingDetail.bitPrimaryGuest = Convert.ToBoolean(PrimaryGuest[i]);
                        bookingDetail.bitActive = true;
                        bookingDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                        bookingDetail.dtCreatedDate = System.DateTime.Now;
                        db.BookingDetails.Add(bookingDetail);
                        db.SaveChanges();
                    }
                    else
                    {
                        if (GuestPP != "")
                        {
                            Guest guest = new Guest();
                            guest.varGuestName = GuestName[i];
                            guest.varGuestEmail = GuestEmail[i] != "" ? GuestEmail[i] : null;
                            guest.varGuestPPNo = GuestPPNo[i];
                            guest.numGuestChargeTypeID = Convert.ToInt32(GuestChargeType[i]);
                            guest.bitActive = true;
                            guest.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                            guest.dtCreatedDate = System.DateTime.Now;
                            db.Guests.Add(guest);
                            db.SaveChanges();

                            BookingDetail bookingDetail = new BookingDetail();
                            bookingDetail.numBookingHeaderID = numBookingID;
                            bookingDetail.numGuestID = guest.numGuestID;
                            bookingDetail.bitPrimaryGuest = Convert.ToBoolean(PrimaryGuest[i]);
                            bookingDetail.bitActive = true;
                            bookingDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                            bookingDetail.dtCreatedDate = System.DateTime.Now;
                            db.BookingDetails.Add(bookingDetail);
                            db.SaveChanges();
                        }
                    }
                }

                decimal numRate = 0;
                decimal numPax = 0;
                int numAdPax = 0;
                int numChiPax = 0;
                int numInfPax = 0;

                var _bookingDetails = db.BookingDetails.Include(g => g.Guest).Include(g => g.Guest.GuestChargeType).Where(d => d.numBookingHeaderID == numBookingID && d.bitActive == true).ToList();
                foreach (var _detail in _bookingDetails)
                {
                    if (_detail.Guest.GuestChargeType.numChargeTypeDiscount.Value == 0)
                    {
                        numPax = numPax + 1;
                        numAdPax = numAdPax + 1;
                    }
                    else if (_detail.Guest.GuestChargeType.numChargeTypeDiscount.Value == 50)
                    {
                        numPax = numPax + Convert.ToDecimal(0.5);
                        numChiPax = numChiPax + 1;
                    }
                    else
                    {
                        numInfPax = numInfPax + 1;
                    }
                }

                if (Convert.ToBoolean(StandardRateBit[0]) == false)
                {
                    var numPaxCount = _bookingDetails.Count();
                    numRate = db.RoomTypeRates.Where(r => r.numRoomTypeID == bookingHeader.RoomType.numRoomTypeID && r.numPersonCount == numPaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                }
                else
                {
                    numRate = Convert.ToDecimal(Rate[0]);
                }

                var billHeader = db.BillHeaders.Where(b => b.numBookingRefferenceID == numRefferenceID && b.numBookingHeaderID == numBookingID && b.bitActive == true).FirstOrDefault();
                var billDetail = db.BillDetails.Where(b => b.numBillHeaderID == billHeader.numBillHeaderID && b.numChargeTypeID == 1 && b.numRoomID == bookingHeader.numRoomID && b.bitActive == true).FirstOrDefault();//Hard Coded Charge Type ID
                decimal NewTotdalDays = Convert.ToDecimal((bookingHeader.dtToDate.Value.Date - dtCheckInDateTime.Date).Days);

                BookingHeader _bookingHeader = db.BookingHeaders.Find(bookingHeader.numBookingHeaderID);

                if (Convert.ToDecimal(billDetail.numQuantity.Value.ToString("0.0")) != numPax || billDetail.numDuration.Value != NewTotdalDays || billDetail.numCost != numRate)
                {
                    _bookingHeader.numAdultsCount = numAdPax;
                    _bookingHeader.numChildrensCount = numChiPax;
                    _bookingHeader.numInfantsCount = numInfPax;

                    billDetail.numDuration = NewTotdalDays;
                    billDetail.numCost = numRate;
                    billDetail.numQuantity = numPax;
                    billDetail.numFinalCost = (billDetail.numCost * billDetail.numDuration) * billDetail.numQuantity;
                    db.Entry(billDetail).State = EntityState.Modified;
                    db.SaveChanges();

                    billHeader.numTotalCost = billDetail.numFinalCost;
                    billHeader.numBalanceToPay = billDetail.numFinalCost - billHeader.numPayedAmount;
                    db.Entry(billHeader).State = EntityState.Modified;
                    db.SaveChanges();
                }

                _bookingHeader.bitStandardRate = Convert.ToBoolean(StandardRateBit[0]);
                if (Convert.ToBoolean(StandardRateBit[0]) == false)
                {
                    _bookingHeader.numRate = null;
                }
                else
                {
                    _bookingHeader.numRate = Convert.ToDecimal(Rate[0]);
                }
                _bookingHeader.bitKSLessonRequired = Convert.ToBoolean(LessonRequiredBit[0]);
                if (Convert.ToBoolean(LessonRequiredBit[0]) == false)
                {
                    _bookingHeader.dtKsLessonRequiredFrom = null;
                    _bookingHeader.dtKsLessonRequiredTo = null;
                }
                else
                {
                    _bookingHeader.dtKsLessonRequiredFrom = Convert.ToDateTime(LessonRequiredsFrom[0]);
                    _bookingHeader.dtKsLessonRequiredTo = Convert.ToDateTime(LessonRequiredsTo[0]);
                }
                _bookingHeader.bitKSRentalRequired = Convert.ToBoolean(RentalRequiredBit[0]);
                if (Convert.ToBoolean(RentalRequiredBit[0]) == false)
                {
                    _bookingHeader.dtKSRentalRequiredFrom = null;
                    _bookingHeader.dtKSRentalRequiredTo = null;
                }
                else
                {
                    _bookingHeader.dtKSRentalRequiredFrom = Convert.ToDateTime(RentalRequiredsFrom[0]);
                    _bookingHeader.dtKSRentalRequiredTo = Convert.ToDateTime(RentalRequiredsTo[0]);
                }
                _bookingHeader.bitAgentRequired = Convert.ToBoolean(AgentRequiredBits[0]);
                if (Convert.ToBoolean(AgentRequiredBits[0]) == false)
                {
                    _bookingHeader.varAgentName = null;
                }
                else
                {
                    _bookingHeader.varAgentName = AgentNames[0];
                }
                _bookingHeader.dtCheckInDateTime = dtCheckInDateTime;
                _bookingHeader.bitCheckedIn = true;
                db.Entry(_bookingHeader).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("CheckIn", "BookingHeaders", new { Msg = "CheckIn" });
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Guest guest = db.Guests.Find(id);
            if (guest == null)
            {
                return HttpNotFound();
            }
            _pmnRecId = guest.numGuestID;
            _pmnStatus = true;
            _pmnUserId = Convert.ToInt32(Session["UserID"]);

            ViewBag.numGuestChargeTypeID = new SelectList(db.GuestChargeTypes.Where(i => i.bitActive == true).OrderBy(x => x.varGuestChargeTypeName), "numGuestChargeTypeID", "varGuestChargeTypeName", guest.numGuestChargeTypeID);
            return PartialView("_Guest", db.Guests.Where(c => c.numGuestID == id).FirstOrDefault<Guest>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Guest guest)
        {
            guest.bitActive = true;
            guest.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            guest.dtUpdatetDate = System.DateTime.Now;

            db.Entry(guest).State = EntityState.Modified;
            db.SaveChanges();

            _pmnRecId = -1;
            _pmnStatus = false;

            TempData["GuestStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Guest guest = db.Guests.Find(id);
            if (guest == null)
            {
                return HttpNotFound();
            }

            guest.bitActive = false;
            guest.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            guest.dtDeletedDate = System.DateTime.Now;

            db.Entry(guest).State = EntityState.Modified;
            db.SaveChanges();

            TempData["GuestStatus"] = "Deleted";
            return RedirectToAction("Index");
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