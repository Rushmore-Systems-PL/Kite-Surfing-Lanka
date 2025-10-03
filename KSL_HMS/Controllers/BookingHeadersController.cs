using KSL_HMS.DB;
using KSL_HMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace KSL_HMS.Controllers
{
    public class BookingHeadersController : Controller
    {
        private readonly ConnectionHMSDB db = new ConnectionHMSDB();

        public ActionResult Index(string varPage)
        {
            if (Session["UserID"] != null)
            {
                if (varPage == "Booking")
                {
                    TempData["BookingConfirmationStatus"] = "";
                    TempData["BookingStatus"] = "";
                }
                else if (varPage == "checkOut")
                {
                    TempData["CheckOutConfirmationStatus"] = "";
                    TempData["BookingStatus"] = "";
                }

                var BookingHeaders = (from bh in db.BookingHeaders
                                      join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                                      where bh.bitActive == true && bf.bitActive == true && bf.bitActive == true && bf.bitClosed == false
                                      select new BookingHeaderDTO
                                      {
                                          numBookingRefferenceID = bf.numBookingRefferenceID,
                                          numBookingHeaderID = bh.numBookingHeaderID,
                                          varBookingRefferenceNo = bf.varBookingRefferenceNo,
                                          bitExternalBooking = bf.bitExternalBooking,
                                          varRoomTypeName = bh.RoomType.varRoomTypeName,
                                          varRoomNo = bh.Room.varRoomNo,
                                          dtFromDate = bh.dtFromDate,
                                          dtToDate = bh.dtToDate,
                                          numGuestCount = bh.numAdultsCount + bh.numChildrensCount + bh.numInfantsCount,
                                          bitClosed = bf.bitClosed,
                                          bitCheckedOut = bh.bitCheckedOut,
                                          bitCheckedIn = bh.bitCheckedIn,
                                          dtCreatedDate = bh.dtCreatedDate
                                      }).OrderByDescending(b => b.dtCreatedDate).ToList();
                return View(BookingHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Create()
        {
            if (Session["UserID"] != null)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpGet]
        public JsonResult LoadRoomCategories(string dtFromDate, string dtToDate, string Bookings, string RemovedBookings)
        {
            if (RemovedBookings == null)
            {
                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                var From = Convert.ToDateTime(dtFromDate);
                var To = Convert.ToDateTime(dtToDate);

                List<int?> BookedRoomIds = new List<int?>();
                if (Bookings == "[]")
                {
                    BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList();
                }
                else
                {
                    var js = new JavaScriptSerializer();
                    List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                    BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                    BookedRoomIds.AddRange(bookings.Where(b => b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                }
                int extRoomTypeID = 7;// External Room Category
                return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.bitActive == true && r.numRoomTypeID != extRoomTypeID).OrderBy(x => x.varRoomName).Select(x => new { id = x.RoomType.numRoomTypeID, name = x.RoomType.varRoomTypeName }).Distinct().ToList(), JsonRequestBehavior.AllowGet);
            }
            else
            {
                var js = new JavaScriptSerializer();
                if (RemovedBookings == "[]")
                {
                    var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                    dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                    dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                    var From = Convert.ToDateTime(dtFromDate);
                    var To = Convert.ToDateTime(dtToDate);

                    List<int?> BookedRoomIds = new List<int?>();
                    if (Bookings == "[]")
                    {
                        BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList();
                    }
                    else
                    {
                        List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                        BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                        BookedRoomIds.AddRange(bookings.Where(b => b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                    }
                    int extRoomTypeID = 7;// External Room Category
                    return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.bitActive == true && r.numRoomTypeID != extRoomTypeID).OrderBy(x => x.varRoomName).Select(x => new { id = x.RoomType.numRoomTypeID, name = x.RoomType.varRoomTypeName }).Distinct().ToList(), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    List<int> NonBookings = new List<int>();
                    NonBookings.AddRange(js.Deserialize<List<int>>(RemovedBookings));

                    var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                    dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                    dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                    var From = Convert.ToDateTime(dtFromDate);
                    var To = Convert.ToDateTime(dtToDate);

                    List<int?> BookedRoomIds = new List<int?>();
                    if (Bookings == "[]")
                    {
                        BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList();
                    }
                    else
                    {
                        List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                        BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList());
                        BookedRoomIds.AddRange(bookings.Where(b => b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                    }
                    int extRoomTypeID = 7;// External Room Category
                    return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.bitActive == true && r.numRoomTypeID != extRoomTypeID).OrderBy(x => x.varRoomName).Select(x => new { id = x.RoomType.numRoomTypeID, name = x.RoomType.varRoomTypeName }).Distinct().ToList(), JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpGet]
        public ActionResult LoadAvailableRooms(int numRoomTypeID, string dtFromDate, string dtToDate, string Bookings, string RemovedBookings)
        {
            if (RemovedBookings == null)
            {
                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                var From = Convert.ToDateTime(dtFromDate);
                var To = Convert.ToDateTime(dtToDate);

                List<int?> BookedRoomIds = new List<int?>();
                if (Bookings == "[]")
                {
                    BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList();
                }
                else
                {
                    var js = new JavaScriptSerializer();
                    List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                    BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                    BookedRoomIds.AddRange(bookings.Where(b => b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                }
                return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.numRoomTypeID == numRoomTypeID && r.bitActive == true).Select(x => new
                {
                    id = x.numRoomID,
                    name = "Room No: " + x.varRoomNo + " | Max Pax: " + x.numMaxPaxAllowed,
                    order = x.varRoomNo
                }).OrderBy(x => x.order).ToList(), JsonRequestBehavior.AllowGet);
            }
            else
            {
                var js = new JavaScriptSerializer();
                if (RemovedBookings == "[]")
                {
                    var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                    dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                    dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                    var From = Convert.ToDateTime(dtFromDate);
                    var To = Convert.ToDateTime(dtToDate);

                    List<int?> BookedRoomIds = new List<int?>();
                    if (Bookings == "[]")
                    {
                        BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList();
                    }
                    else
                    {
                        List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                        BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                        BookedRoomIds.AddRange(bookings.Where(b => b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                    }
                    return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.numRoomTypeID == numRoomTypeID && r.bitActive == true).Select(x => new
                    {
                        id = x.numRoomID,
                        name = "Room No: " + x.varRoomNo + " | Max Pax: " + x.numMaxPaxAllowed,
                        order = x.varRoomNo
                    }).OrderBy(x => x.order).ToList(), JsonRequestBehavior.AllowGet);
                }
                else
                {
                    List<int> NonBookings = new List<int>();
                    NonBookings.AddRange(js.Deserialize<List<int>>(RemovedBookings));

                    var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                    dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                    dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                    var From = Convert.ToDateTime(dtFromDate);
                    var To = Convert.ToDateTime(dtToDate);

                    List<int?> BookedRoomIds = new List<int?>();
                    if (Bookings == "[]")
                    {
                        BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList();
                    }
                    else
                    {
                        List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                        BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList());
                        BookedRoomIds.AddRange(bookings.Where(b => b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
                    }
                    return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.numRoomTypeID == numRoomTypeID && r.bitActive == true).Select(x => new
                    {
                        id = x.numRoomID,
                        name = "Room No: " + x.varRoomNo + " | Max Pax: " + x.numMaxPaxAllowed,
                        order = x.varRoomNo
                    }).OrderBy(x => x.order).ToList(), JsonRequestBehavior.AllowGet);
                }
            }
        }

        [HttpGet]
        public ActionResult LoadRoomPaxCount(int numRoomID)
        {
            return Json(db.Rooms.Where(r => r.numRoomID == numRoomID && r.bitActive == true).Select(r => r.numMaxPaxAllowed).FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }

        // Confirmation screen bypassed, functionality of create and confirm merged into one.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection formCollection, BookingHeader newbookingHeader)
        {
            try
            {
                var bookingPersonName = newbookingHeader.varBookingPersonName.Replace(" ", "").ToUpper().PadLeft(10, '#').Substring(0, 10);
                int numBookingNO = Convert.ToInt32(db.BookingRefferences.Where(b => b.bitActive == true && b.bitExternalBooking == false).Count()) + 1;
                string varRef = "KSL/INT/" + numBookingNO.ToString("00000") + "/" + bookingPersonName;

                var newbookingRefference = new BookingRefference()
                {
                    varBookingRefferenceNo = varRef,
                    bitExternalBooking = false,
                    numCreatedByID = Convert.ToInt32(Session["UserID"]),
                    dtCreatedDate = System.DateTime.Now
                };
                db.BookingRefferences.Add(newbookingRefference);
                db.SaveChanges();

                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                var FromDates = formCollection.GetValues("dtFromDate");
                var ToDates = formCollection.GetValues("dtToDate");
                var RoomTypeIDs = formCollection.GetValues("numRoomTypeID");
                var RoomIDs = formCollection.GetValues("numRoomID");
                var StandardRateBits = formCollection.GetValues("bitStandardRateValue");
                var Rates = formCollection.GetValues("numRate");
                var TransportRequiredBits = formCollection.GetValues("bitTransportRequiredValue");
                var FlightDetails = formCollection.GetValues("varFlightDetails");
                var LessonRequiredBits = formCollection.GetValues("bitKSLessonRequiredValue");
                var LessonRequiredsFrom = formCollection.GetValues("dtKsLessonRequiredFrom");
                var LessonRequiredsTo = formCollection.GetValues("dtKsLessonRequiredTo");
                var RentalRequiredBits = formCollection.GetValues("bitKSRentalRequiredValue");
                var RentalRequiredsFrom = formCollection.GetValues("dtKSRentalRequiredFrom");
                var RentalRequiredsTo = formCollection.GetValues("dtKSRentalRequiredTo");
                var AgentRequiredBits = formCollection.GetValues("bitKSAgentRequiredValue");
                var AgentNames = formCollection.GetValues("varAgentName");
                var Remarks = formCollection.GetValues("varRemarks");
                var AdultsCounts = formCollection.GetValues("numAdultsCount");
                var ChildrensCounts = formCollection.GetValues("numChildrensCount");
                var InfantsCounts = formCollection.GetValues("numInfantsCount");

                for (int i = RoomIDs.Count() - 1; i >= 0; i--)
                {
                    BookingHeader bookingHeader = new BookingHeader();
                    bookingHeader.numBookingRefferenceID = newbookingRefference.numBookingRefferenceID;
                    bookingHeader.dtFromDate = Convert.ToDateTime(FromDates[i] + " " + RoomcheckInouts[0].varRoomCheckInTime);
                    bookingHeader.dtToDate = Convert.ToDateTime(ToDates[i] + " " + RoomcheckInouts[0].varRoomCheckOutTime);
                    bookingHeader.numRoomTypeID = Convert.ToInt32(RoomTypeIDs[i]);
                    bookingHeader.numRoomID = Convert.ToInt32(RoomIDs[i]);
                    //Rates
                    bookingHeader.bitStandardRate = Convert.ToBoolean(StandardRateBits[i]);
                    if (!Convert.ToBoolean(StandardRateBits[i]))
                    {
                        bookingHeader.numRate = null;
                    }
                    else
                    {
                        bookingHeader.numRate = Convert.ToDecimal(Rates[i]);
                    }
                    //Transport
                    bookingHeader.bitTransportRequired = Convert.ToBoolean(TransportRequiredBits[i]);
                    if (!Convert.ToBoolean(TransportRequiredBits[i]))
                    {
                        bookingHeader.varFlightDetails = null;
                    }
                    else
                    {
                        bookingHeader.varFlightDetails = FlightDetails[i];
                    }
                    //Lesson
                    bookingHeader.bitKSLessonRequired = Convert.ToBoolean(LessonRequiredBits[i]);
                    if (!Convert.ToBoolean(LessonRequiredBits[i]))
                    {
                        bookingHeader.dtKsLessonRequiredFrom = null;
                        bookingHeader.dtKsLessonRequiredTo = null;
                    }
                    else
                    {
                        bookingHeader.dtKsLessonRequiredFrom = Convert.ToDateTime(LessonRequiredsFrom[i]);
                        bookingHeader.dtKsLessonRequiredTo = Convert.ToDateTime(LessonRequiredsTo[i]);
                    }
                    //Rental
                    bookingHeader.bitKSRentalRequired = Convert.ToBoolean(RentalRequiredBits[i]);
                    if (!Convert.ToBoolean(RentalRequiredBits[i]))
                    {
                        bookingHeader.dtKSRentalRequiredFrom = null;
                        bookingHeader.dtKSRentalRequiredTo = null;
                    }
                    else
                    {
                        bookingHeader.dtKSRentalRequiredFrom = Convert.ToDateTime(RentalRequiredsFrom[i]);
                        bookingHeader.dtKSRentalRequiredTo = Convert.ToDateTime(RentalRequiredsTo[i]);
                    }
                    //Agent
                    bookingHeader.bitAgentRequired = Convert.ToBoolean(AgentRequiredBits[i]);
                    if (!Convert.ToBoolean(AgentRequiredBits[i]))
                    {
                        bookingHeader.varAgentName = null;
                    }
                    else
                    {
                        bookingHeader.varAgentName = AgentNames[i];
                    }
                    bookingHeader.varRemarks = Remarks[i];
                    bookingHeader.varBookingPersonName = newbookingHeader.varBookingPersonName;
                    bookingHeader.varBookingPersonEmail = newbookingHeader.varBookingPersonEmail;
                    bookingHeader.numAdultsCount = Convert.ToInt32(AdultsCounts[i]);
                    bookingHeader.numChildrensCount = Convert.ToInt32(ChildrensCounts[i]);
                    bookingHeader.numInfantsCount = Convert.ToInt32(InfantsCounts[i]);
                    bookingHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    bookingHeader.dtCreatedDate = System.DateTime.Now;
                    db.BookingHeaders.Add(bookingHeader);
                    db.SaveChanges();
                }

                BookingRefference bookingRefference = db.BookingRefferences.Find(newbookingRefference.numBookingRefferenceID);
                if (bookingRefference == null)
                {
                    return HttpNotFound();
                }
                if (bookingRefference.bitActive != true)
                {
                    bookingRefference.bitClosed = false;
                    bookingRefference.bitPayed = false;
                    bookingRefference.bitActive = true;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();

                    var BookingHeaders = db.BookingHeaders.Where(h => h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).OrderByDescending(h => h.numBookingHeaderID).ToList();
                    foreach (var _header in BookingHeaders)
                    {
                        decimal numTotalCost = 0;
                        decimal numRate = 0;
                        decimal numPax = 0;

                        BookingHeader bookingHeader = db.BookingHeaders.Find(_header.numBookingHeaderID);
                        bookingHeader.bitActive = true;
                        bookingHeader.bitCheckedIn = false;
                        bookingHeader.bitCheckedOut = false;
                        db.Entry(bookingHeader).State = EntityState.Modified;
                        db.SaveChanges();

                        int numTotalDays = ((_header.dtToDate.Value.Date - _header.dtFromDate.Value.Date).Days);
                        numPax = Convert.ToDecimal(bookingHeader.numAdultsCount) + (Convert.ToDecimal(bookingHeader.numChildrensCount) * Convert.ToDecimal(0.5));

                        if (_header.bitStandardRate == false)
                        {
                            var numPaxCount = bookingHeader.numAdultsCount + bookingHeader.numChildrensCount + bookingHeader.numInfantsCount;
                            numRate = db.RoomTypeRates.Where(r => r.numRoomTypeID == _header.numRoomTypeID && r.numPersonCount == numPaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                            numTotalCost = (numRate * numPax);
                        }
                        else
                        {
                            numRate = _header.numRate.Value;
                            numTotalCost = (numRate * numPax);
                        }

                        BillHeader billHeader = new BillHeader();
                        billHeader.numBookingRefferenceID = bookingRefference.numBookingRefferenceID;
                        billHeader.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                        billHeader.numTotalCost = numTotalCost * numTotalDays;
                        billHeader.numPayedAmount = 0;
                        billHeader.numBalanceToPay = numTotalCost * numTotalDays;
                        billHeader.bitActive = true;
                        billHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                        billHeader.dtCreatedDate = System.DateTime.Now;
                        db.BillHeaders.Add(billHeader);
                        db.SaveChanges();

                        BillDetail billDetail = new BillDetail();
                        billDetail.numBillHeaderID = billHeader.numBillHeaderID;
                        billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Room").Select(c => c.numChargeTypeID).FirstOrDefault();
                        billDetail.numRoomID = bookingHeader.numRoomID;
                        billDetail.numCost = numRate;
                        billDetail.numDuration = numTotalDays;
                        billDetail.numQuantity = numPax;
                        billDetail.numFinalCost = (numRate * numTotalDays) * numPax;
                        billDetail.bitActive = true;
                        billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                        billDetail.dtCreatedDate = System.DateTime.Now;
                        db.BillDetails.Add(billDetail);
                        db.SaveChanges();
                    }
                    //this.NewBookingEmailForGuest(bookingRefference.numBookingRefferenceID);
                    //this.NewBookingEmailForAdmin(bookingRefference.numBookingRefferenceID);
                }

                TempData["BookingHeaderStatus"] = "Saved";
                return RedirectToAction("Index", new { varPage = "Booking" });
            }
            catch (Exception ex) {
                return null;
            }
        }

        public void NewBookingEmailForGuest(int? id)
        {
            BookingRefference bookingRefference = db.BookingRefferences.Find(id);
            try
            {
                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                var bookings = db.BookingHeaders.Where(h => h.bitActive == true && h.numBookingRefferenceID == id).ToList();

                string header = "<!DOCTYPE html><html><head><style>body {font-family: 'Public Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue', sans-serif;font-size: 20px;background-color: rgba(245, 245, 249);}h3 {color: #566a7f;}p {color: #566a7f;}li {color: #566a7f;}</style><head>" +
                                "<body><h3>Hi " + bookings[0].varBookingPersonName + ",</h3><p>Thank you for choosing Kitesurfing Lanka.We look forward to hosting your stay. This is confirmation for your reservation at Kitesurfing Lanka. Your booking information is as follows.</p><h3>Booking Details</h3>";
                StringBuilder body = new StringBuilder();
                int i = 0;
                decimal rate = 0;
                decimal numPax = 0;
                foreach (var item in bookings)
                {
                    numPax = Convert.ToDecimal(item.numAdultsCount) + (Convert.ToDecimal(item.numChildrensCount) * Convert.ToDecimal(0.5));
                    if (item.bitStandardRate == false)
                    {
                        var numPaxCount = item.numAdultsCount + item.numChildrensCount + item.numInfantsCount;
                        rate = db.RoomTypeRates.Where(r => r.numRoomTypeID == item.numRoomTypeID && r.numPersonCount == numPaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                        rate = rate * numPax;
                    }
                    else
                    {
                        rate = item.numRate.Value * numPax;
                    }

                    if (i > 0)
                    {
                        if (item.numChildrensCount != 0)
                        {
                            body = body.Append($"<ul><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li><li>No of Children: <b>{item.numChildrensCount.Value.ToString("00")}</b></li><li>Room Rate: <b>€ {rate.ToString("0.00")}</b></li></ul>");
                        }
                        else
                        {
                            body = body.Append($"<ul><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li><li>Room Rate: <b>€ {rate.ToString("0.00")}</b></li></ul>");
                        }
                    }
                    else
                    {
                        if (item.numChildrensCount != 0)
                        {
                            body = body.Append($"<ul><li>Reservation Confirmation Number: <b>{bookingRefference.varBookingRefferenceNo.Substring(0, bookingRefference.varBookingRefferenceNo.Length - 11)}</b></li><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li><li>No of Children: <b>{item.numChildrensCount.Value.ToString("00")}</b></li><li>Room Rate: <b>€ {rate.ToString("0.00")}</b></li></ul>");
                        }
                        else
                        {
                            body = body.Append($"<ul><li>Reservation Confirmation Number: <b>{bookingRefference.varBookingRefferenceNo.Substring(0, bookingRefference.varBookingRefferenceNo.Length - 11)}</b></li><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li><li>Room Rate: <b>€ {rate.ToString("0.00")}</b></li></ul>");
                        }
                    }
                    i++;
                }

                string footer = "<h3>Booking  Policy</h3><ul><li>Check-in is at " + Convert.ToDateTime(RoomcheckInouts[0].varRoomCheckInTime).ToString("hh tt").ToLower() + " IST on the date of your arrival and check-out is at " + Convert.ToDateTime(RoomcheckInouts[0].varRoomCheckInTime).ToString("hh tt").ToLower() + " IST on departure day.</li><li>Rate applies only to the standard offering per basis booked. All extras will be charged accordingly and are not reflected in the rate above.</li><li>Please reach us through our hotline or email for any changes or any questions.</li>" +
                                "<li>Cancellations received with 02 days or more notice, will be refunded in full less any bank charges, (If any).</li><li>As our child policy 0 – 2 Years, Free of charge and 2 - 12 Years, 50% of adult Rate.</li><li>To confirm the reservation, We accept Credit Card Payment, PayPal or Bank Transfer.</li></ul>" +
                                 "<p>Wishing you a pleasant stay at Kitesurfing Lanka.<p/><p><b>Kitesurfing Lanka</b>,<br/>Dutch Bay Road (Kudawa Road),<br/>Kandakuliya Beach Road, Kalpitiya,<br/>61360, Sri Lanka.<br/>+ 94 77 368 6235, +33 78 330 0803</p><p><b>Note: </b>This is an automatically generated email by the Kitesurfing Lanka. Please do not reply.</p></body></html>";

                MailMessage message = new MailMessage();
                message.From = new MailAddress("admin@ksloffice.com");
                message.To.Add(new MailAddress(bookings[0].varBookingPersonEmail));
                message.Subject = "Kitesurfing Lanka Alert | New Reservation | " + System.DateTime.Now.ToString("dddd, dd MMMM yyyy");
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(header + body + footer, null, "text/html");
                message.AlternateViews.Add(htmlView);
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "relay-hosting.secureserver.net";
                smtpClient.Port = 25;
                smtpClient.EnableSsl = false;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = new System.Net.NetworkCredential("admin@ksloffice.com", "KSLoffice123!@#");
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress("admin@ksloffice.com");
                message.To.Add(new MailAddress("admin@ksloffice.com"));
                message.Subject = "Kitesurfing Lanka Alert | New Reservation Error Alert For Guest | " + System.DateTime.Now.ToString("dddd, dd MMMM yyyy");
                message.Body = "Reference No : " + bookingRefference.varBookingRefferenceNo + "<br/>Mail Not Sent On: " + ex.Message.ToString();
                message.IsBodyHtml = true;
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "relay-hosting.secureserver.net";
                smtpClient.Port = 25;
                smtpClient.EnableSsl = false;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = new System.Net.NetworkCredential("admin@ksloffice.com", "KSLoffice123!@#");
                smtpClient.Send(message);
            }
        }

        public void NewBookingEmailForAdmin(int? id)
        {
            BookingRefference bookingRefference = db.BookingRefferences.Find(id);
            try
            {
                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                var bookings = db.BookingHeaders.Where(h => h.bitActive == true && h.numBookingRefferenceID == id).ToList();

                string header = "<!DOCTYPE html><html><head><style>body {font-family: 'Public Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue', sans-serif;font-size: 20px;background-color: rgba(245, 245, 249);}h3 {color: #566a7f;}p {color: #566a7f;}li {color: #566a7f;}</style><head>" +
                                "<body><h3>Hi Team,</h3><p>The following reservation/reservations has made for Kitesurfing Lanka.</p><h3>Booking Details</h3>";
                StringBuilder body = new StringBuilder();
                int i = 0;
                foreach (var item in bookings)
                {
                    if (i > 0)
                    {
                        if (item.numChildrensCount != 0)
                        {
                            body = body.Append($"<ul><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li><li>No of Children: <b>{item.numChildrensCount.Value.ToString("00")}</b></li></ul>");
                        }
                        else
                        {
                            body = body.Append($"<ul><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li></ul>");
                        }
                    }
                    else
                    {
                        if (item.numChildrensCount != 0)
                        {
                            body = body.Append($"<ul><li>Reservation Confirmation Number: <b>{bookingRefference.varBookingRefferenceNo.Substring(0, bookingRefference.varBookingRefferenceNo.Length - 11)}</b></li><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li><li>No of Children: <b>{item.numChildrensCount.Value.ToString("00")}</b></li></ul>");
                        }
                        else
                        {
                            body = body.Append($"<ul><li>Reservation Confirmation Number: <b>{bookingRefference.varBookingRefferenceNo.Substring(0, bookingRefference.varBookingRefferenceNo.Length - 11)}</b></li><li>Room Category: <b>{item.RoomType.varRoomTypeName}</b></li><li>Room No: <b>{item.Room.varRoomNo}</b></li><li>Arrival date: <b>{item.dtFromDate.Value.ToString("yyyy-MM-dd")}</b></li><li>Departure date: <b>{item.dtToDate.Value.ToString("yyyy-MM-dd")}</b></li><li>No of Adults: <b>{item.numAdultsCount.Value.ToString("00")}</b></li></ul>");
                        }
                    }
                    i++;
                }
                string footer = "<p><b>Note: </b>This is an automatically generated email by the Kitesurfing Lanka. Please do not reply.</p></body></html>";

                MailMessage message = new MailMessage();
                message.From = new MailAddress("admin@ksloffice.com");
                message.To.Add(new MailAddress("admin@ksloffice.com"));
                message.Subject = "Kitesurfing Lanka Alert | New Reservation | " + System.DateTime.Now.ToString("dddd, dd MMMM yyyy");
                AlternateView htmlView = AlternateView.CreateAlternateViewFromString(header + body + footer, null, "text/html");
                message.AlternateViews.Add(htmlView);
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "relay-hosting.secureserver.net";
                smtpClient.Port = 25;
                smtpClient.EnableSsl = false;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = new System.Net.NetworkCredential("admin@ksloffice.com", "KSLoffice123!@#");
                smtpClient.Send(message);
            }
            catch (Exception ex)
            {
                MailMessage message = new MailMessage();
                message.From = new MailAddress("admin@ksloffice.com");
                message.To.Add(new MailAddress("admin@ksloffice.com"));
                message.Subject = "Kitesurfing Lanka Alert | New Reservation Error Alert For Admin | " + System.DateTime.Now.ToString("dddd, dd MMMM yyyy");
                message.Body = "Reference No : " + bookingRefference.varBookingRefferenceNo + "<br/>Mail Not Sent On: " + ex.Message.ToString();
                message.IsBodyHtml = true;
                SmtpClient smtpClient = new SmtpClient();
                smtpClient.Host = "relay-hosting.secureserver.net";
                smtpClient.Port = 25;
                smtpClient.EnableSsl = false;
                smtpClient.UseDefaultCredentials = true;
                smtpClient.Credentials = new System.Net.NetworkCredential("admin@ksloffice.com", "KSLoffice123!@#");
                smtpClient.Send(message);
            }
        }

        public ActionResult CheckIn(string Msg)
        {
            if (Session["UserID"] != null)
            {
                if (Msg == "CheckIn")
                {
                    TempData["CheckInConfirmationStatus"] = "Saved";
                }

                var Today = System.DateTime.Now.Date;
                var CheckInRoomIDs = db.BookingHeaders.Where(bh => bh.bitCheckedIn == true && bh.bitCheckedOut == false && bh.bitActive == true && bh.BookingRefference.bitExternalBooking == false).Select(r => r.numRoomID).ToList();
                var Bookings = db.BookingHeaders.Where(bh => bh.bitCheckedIn == false &&
                                                             bh.bitActive == true &&
                                                             bh.BookingRefference.bitExternalBooking == false &&
                                                             DbFunctions.TruncateTime(bh.dtToDate) >= DbFunctions.TruncateTime(Today) &&
                                                             !CheckInRoomIDs.Contains(bh.numRoomID)).Select(x => new
                                                             {
                                                                 Bid = x.numBookingHeaderID,
                                                                 Bdesc = "REF : " + x.BookingRefference.varBookingRefferenceNo + " | ROOM NO: " + x.Room.varRoomNo,
                                                                 Border = x.dtCreatedDate
                                                             }).OrderByDescending(bh => bh.Border);
                ViewBag.BookingID = new SelectList(Bookings, "Bid", "Bdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult CheckOut(string Msg)
        {
            if (Session["UserID"] != null)
            {
                if (Msg == "CheckOut")
                {
                    TempData["CheckOutConfirmationStatus"] = "Saved";
                }
                var Today = System.DateTime.Now.Date;
                var Bookings = (from bh in db.BookingHeaders
                                join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                                where
                                bh.bitCheckedIn == true && bh.bitActive == true && bf.bitExternalBooking == false && bh.bitCheckedOut == false
                                select new
                                {
                                    Bid = bh.numBookingHeaderID,
                                    Bdesc = "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo,
                                    Border = bh.dtCreatedDate
                                }).OrderBy(x => x.Border).ToList();
                ViewBag.BookingID = new SelectList(Bookings, "Bid", "Bdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult BookingCheckOutDetails(int? id)
        {
            if (Session["UserID"] != null)
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
                var BookingHeaders = db.BookingHeaders.Where(h => h.bitActive == true && h.numBookingHeaderID == bookingHeader.numBookingHeaderID).OrderBy(x => x.dtCreatedDate).ToList();
                var BookingHeaderIDs = BookingHeaders.Select(h => h.numBookingHeaderID).ToList();
                var BookingDetails = db.BookingDetails.Where(d => d.bitActive == true && BookingHeaderIDs.Contains(d.numBookingHeaderID.Value)).OrderBy(x => x.dtCreatedDate).ToList();
                ViewBag.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                ViewBag.GuestCount = BookingDetails.Count;
                ViewBag.RoomsCount = BookingHeaders.Count;
                ViewBag.BookingHeaders = BookingHeaders;
                ViewBag.BookingDetails = BookingDetails;
                ViewBag.BillDetails = db.BillDetails.Where(b => b.BillHeader.numBookingHeaderID == bookingHeader.numBookingHeaderID && b.BillHeader.BookingRefference.bitActive == true && b.BillHeader.BookingRefference.bitClosed == false && b.BillHeader.bitActive == true && b.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                ViewBag.Receipts = db.Receipts.Where(r => r.bitActive == true && r.numBookingHeaderID == bookingHeader.numBookingHeaderID).OrderBy(x => x.dtCreatedDate).ToList();
                return PartialView("_BookingCheckout", db.BillHeaders.Where(c => c.numBookingHeaderID == id).FirstOrDefault<BillHeader>());
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        public ActionResult ConfirmCheckOut(int? id)
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
            var billHeader = db.BillHeaders.Where(b => b.numBookingRefferenceID == bookingHeader.numBookingRefferenceID && b.numBookingHeaderID == bookingHeader.numBookingHeaderID && b.bitActive == true).First();
            var billDetail = db.BillDetails.Where(b => b.numBillHeaderID == billHeader.numBillHeaderID && b.numChargeTypeID == 1 && b.numRoomID == bookingHeader.numRoomID && b.bitActive == true).First();//Hard Coded Charge Type ID

            decimal oldDuration = billDetail.numDuration.Value;
            decimal endingDurationModifier = (System.DateTime.Now.Date - bookingHeader.dtToDate.Value.Date).Days;
            decimal newDuration = (oldDuration + endingDurationModifier) <= 0 ? 1 : (oldDuration + endingDurationModifier);

            decimal numOldFinalCost = billDetail.numFinalCost.Value;

            billDetail.numDuration = newDuration;
            billDetail.numFinalCost = (billDetail.numCost * billDetail.numDuration) * billDetail.numQuantity;
            db.Entry(billDetail).State = EntityState.Modified;
            db.SaveChanges();

            billHeader.numTotalCost = (billHeader.numTotalCost - numOldFinalCost) + billDetail.numFinalCost;
            billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            bookingHeader.dtCheckOutDateTime = System.DateTime.Now;
            bookingHeader.bitCheckedOut = true;
            db.Entry(bookingHeader).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("CheckOut", new { Msg = "CheckOut" });
        }

        [HttpPost]
        public ActionResult Delete(int? id)
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

            var BookingHeaders = db.BookingHeaders.Where(h => h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).ToList();
            if (BookingHeaders.Count != 0)
            {
                db.BookingHeaders.RemoveRange(BookingHeaders);
                db.SaveChanges();
            }
            db.BookingRefferences.Remove(bookingRefference);
            db.SaveChanges();

            TempData["BookingStatus"] = "Deleted";
            return RedirectToAction("Create");
        }

        [HttpPost]
        public ActionResult DeActive(int? id)
        {
            var Booking = db.BookingHeaders.Where(b => b.numBookingHeaderID == id).First();
            var numReffID = Booking.numBookingRefferenceID;

            var bookings = db.BookingHeaders.Where(b => b.numBookingRefferenceID == numReffID && b.bitActive == true).ToList();
            if (bookings.Count == 1)
            {
                // Abort deletion if receipts exist!!!
                List<Receipt> receipts = db.Receipts.Where(r => r.bitActive == true && r.numBookingRefferenceID == numReffID).ToList();
                if (receipts.Count > 0) {
                    TempData["BookingStatus"] = "Aborted";
                    return RedirectToAction("Index");
                }
            }
            else {
                // Convert all single room receipts to reference receipts for that booking
                List<Receipt> receipts = db.Receipts.Where(r => r.bitActive == true && r.bitSingleBooking == true && r.numBookingHeaderID == id).ToList();
                foreach (Receipt receipt in receipts)
                {
                    receipt.bitSingleBooking = false;
                    receipt.numBookingHeaderID = null;
                    receipt.numBillHeaderID = null;
                    receipt.bitActive = true;
                    receipt.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                    receipt.dtUpdatetDate = System.DateTime.Now;
                    db.Entry(receipt).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

                
            var BillHeader = db.BillHeaders.Where(bh => bh.numBookingRefferenceID == numReffID && bh.numBookingHeaderID == id).First();

            var BillDetails = db.BillDetails.Where(bd => bd.numBillHeaderID == BillHeader.numBillHeaderID).ToList();
            if (BillDetails.Count != 0)
            {
                db.BillDetails.RemoveRange(BillDetails);
                db.SaveChanges();
            }

            db.BillHeaders.Remove(BillHeader);
            db.SaveChanges();

            db.BookingHeaders.Remove(Booking);
            db.SaveChanges();

            bookings = db.BookingHeaders.Where(b => b.numBookingRefferenceID == numReffID && b.bitActive == true).ToList();
            if (bookings.Count == 0)
            {
                var NotActiveBookings = db.BookingHeaders.Where(b => b.numBookingRefferenceID == numReffID).ToList();
                if (NotActiveBookings.Count != 0)
                {
                    db.BookingHeaders.RemoveRange(NotActiveBookings);
                    db.SaveChanges();
                }

                db.BookingRefferences.Remove(db.BookingRefferences.Where(b => b.numBookingRefferenceID == numReffID).First());
                db.SaveChanges();
            }

            //this.DeactivateBookingEmailForGuest(bookingRefference.numBookingRefferenceID);
            //this.DeactivateBookingEmailForAdmin(bookingRefference.numBookingRefferenceID);

            TempData["BookingStatus"] = "DeActivate";
            return RedirectToAction("Index");
        }

        public ActionResult CreateExternal()
        {
            if (Session["UserID"] != null)
            {
                ViewBag.numGuestChargeTypeID = new SelectList(db.GuestChargeTypes.Where(g => g.bitActive == true).OrderBy(x => x.varGuestChargeTypeName).ToList(), "numGuestChargeTypeID", "varGuestChargeTypeName");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateExternal(BookingHeader bookingHeader)
        {
            var bookingPersonName = bookingHeader.varGuestName.Replace(" ", "").ToUpper().PadLeft(10, '#').Substring(0, 10);
            int numBookingNO = Convert.ToInt32(db.BookingRefferences.Where(b => b.bitActive == true && b.bitExternalBooking == true).Count()) + 1;
            string varRef = "KSL/EXT/" + numBookingNO.ToString("00000") + "/" + bookingPersonName;

            var newbookingRefference = new BookingRefference()
            {
                varBookingRefferenceNo = varRef,
                bitExternalBooking = true,
                bitClosed = false,
                bitPayed = false,
                bitActive = true,
                numCreatedByID = Convert.ToInt32(Session["UserID"]),
                dtCreatedDate = System.DateTime.Now
            };
            db.BookingRefferences.Add(newbookingRefference);
            db.SaveChanges();

            Room _room = db.Rooms.Where(r => r.varRoomName == "External Guest" && r.bitActive == true).First();
            bookingHeader.numBookingRefferenceID = newbookingRefference.numBookingRefferenceID;
            bookingHeader.dtFromDate = System.DateTime.Now;
            bookingHeader.dtToDate = System.DateTime.Now;
            bookingHeader.numRoomTypeID = _room.numRoomTypeID;
            bookingHeader.numRoomID = _room.numRoomID;

            if (bookingHeader.bitTransportRequired == false)
            {
                bookingHeader.varFlightDetails = null;
            }

            if (bookingHeader.bitKSLessonRequired == false)
            {
                bookingHeader.dtKsLessonRequiredFrom = null;
                bookingHeader.dtKsLessonRequiredTo = null;
            }

            if (bookingHeader.bitKSRentalRequired == false)
            {
                bookingHeader.dtKSRentalRequiredFrom = null;
                bookingHeader.dtKSRentalRequiredTo = null;
            }

            bookingHeader.bitStandardRate = false;
            bookingHeader.bitActive = true;
            bookingHeader.bitCheckedIn = true;
            bookingHeader.dtCheckInDateTime = System.DateTime.Now;
            bookingHeader.bitCheckedOut = false;
            bookingHeader.numAdultsCount = 1;
            bookingHeader.numChildrensCount = 0;
            bookingHeader.numInfantsCount = 0;
            bookingHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            bookingHeader.dtCreatedDate = System.DateTime.Now;
            db.BookingHeaders.Add(bookingHeader);
            db.SaveChanges();

            var GuestId = db.Guests.Where(g => g.bitActive == true && g.varGuestPPNo.ToUpper() == bookingHeader.varGuestPPNo.ToUpper()).Select(g => g.numGuestID).FirstOrDefault();
            if (GuestId != 0)
            {
                Guest exguest = db.Guests.Find(GuestId);
                if (exguest.varGuestName != bookingHeader.varGuestName || exguest.varGuestEmail != bookingHeader.varGuestEmail || exguest.numGuestChargeTypeID != bookingHeader.numGuestChargeTypeID)
                {
                    exguest.varGuestName = bookingHeader.varGuestName;
                    exguest.varGuestEmail = bookingHeader.varGuestEmail;
                    exguest.numGuestChargeTypeID = bookingHeader.numGuestChargeTypeID;
                    exguest.bitActive = true;
                    exguest.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                    exguest.dtUpdatetDate = System.DateTime.Now;

                    db.Entry(exguest).State = EntityState.Modified;
                    db.SaveChanges();
                }

                BookingDetail bookingDetail = new BookingDetail();
                bookingDetail.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                bookingDetail.numGuestID = GuestId;
                bookingDetail.bitPrimaryGuest = true;
                bookingDetail.bitActive = true;
                bookingDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                bookingDetail.dtCreatedDate = System.DateTime.Now;
                db.BookingDetails.Add(bookingDetail);
                db.SaveChanges();
            }
            else
            {
                if (bookingHeader.varGuestPPNo != "")
                {
                    Guest guest = new Guest();
                    guest.varGuestName = bookingHeader.varGuestName;
                    guest.varGuestEmail = bookingHeader.varGuestEmail;
                    guest.varGuestPPNo = bookingHeader.varGuestPPNo;
                    guest.numGuestChargeTypeID = bookingHeader.numGuestChargeTypeID;
                    guest.bitActive = true;
                    guest.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    guest.dtCreatedDate = System.DateTime.Now;
                    db.Guests.Add(guest);
                    db.SaveChanges();

                    BookingDetail bookingDetail = new BookingDetail();
                    bookingDetail.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                    bookingDetail.numGuestID = guest.numGuestID;
                    bookingDetail.bitPrimaryGuest = true;
                    bookingDetail.bitActive = true;
                    bookingDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    bookingDetail.dtCreatedDate = System.DateTime.Now;
                    db.BookingDetails.Add(bookingDetail);
                    db.SaveChanges();
                }
            }

            BillHeader billHeader = new BillHeader();
            billHeader.numBookingRefferenceID = newbookingRefference.numBookingRefferenceID;
            billHeader.numBookingHeaderID = bookingHeader.numBookingHeaderID;
            billHeader.numTotalCost = 0;
            billHeader.numPayedAmount = 0;
            billHeader.numBalanceToPay = 0;
            billHeader.bitActive = true;
            billHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            billHeader.dtCreatedDate = System.DateTime.Now;
            db.BillHeaders.Add(billHeader);
            db.SaveChanges();

            TempData["BookingExternalStatus"] = "Saved";
            return RedirectToAction("CreateExternal");
        }

        public ActionResult AddBooking()
        {
            if (Session["UserID"] != null)
            {
                ViewBag.numBookingRefferenceID = new SelectList(db.BookingRefferences.Where(r => r.bitActive == true && r.bitClosed == false && r.bitPayed == false && r.bitExternalBooking == false).OrderBy(x => x.dtCreatedDate), "numBookingRefferenceID", "varBookingRefferenceNo");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBooking(FormCollection formCollection)
        {
            var varBookingRefferenceID = formCollection.GetValues("numBookingRefferenceID");
            var FromDates = formCollection.GetValues("dtFromDate");
            var ToDates = formCollection.GetValues("dtToDate");
            var RoomTypeIDs = formCollection.GetValues("numRoomTypeID");
            var RoomIDs = formCollection.GetValues("numRoomID");
            var StandardRateBits = formCollection.GetValues("bitStandardRateValue");
            var Rates = formCollection.GetValues("numRate");
            var TransportRequiredBits = formCollection.GetValues("bitTransportRequiredValue");
            var FlightDetails = formCollection.GetValues("varFlightDetails");
            var LessonRequiredBits = formCollection.GetValues("bitKSLessonRequiredValue");
            var LessonRequiredsFrom = formCollection.GetValues("dtKsLessonRequiredFrom");
            var LessonRequiredsTo = formCollection.GetValues("dtKsLessonRequiredTo");
            var RentalRequiredBits = formCollection.GetValues("bitKSRentalRequiredValue");
            var RentalRequiredsFrom = formCollection.GetValues("dtKSRentalRequiredFrom");
            var RentalRequiredsTo = formCollection.GetValues("dtKSRentalRequiredTo");
            var AgentRequiredBits = formCollection.GetValues("bitKSAgentRequiredValue");
            var AgentNames = formCollection.GetValues("varAgentName");
            var Remarks = formCollection.GetValues("varRemarks");
            var AdultsCounts = formCollection.GetValues("numAdultsCount");
            var ChildrensCounts = formCollection.GetValues("numChildrensCount");
            var InfantsCounts = formCollection.GetValues("numInfantsCount");

            for (int i = 0; i < RoomIDs.Count(); i++)
            {
                BookingHeader bookingHeader = new BookingHeader();
                bookingHeader.numBookingRefferenceID = Convert.ToInt32(varBookingRefferenceID[0]);
                bookingHeader.dtFromDate = Convert.ToDateTime(FromDates[i]);
                bookingHeader.dtToDate = Convert.ToDateTime(ToDates[i]);
                bookingHeader.numRoomTypeID = Convert.ToInt32(RoomTypeIDs[i]);
                bookingHeader.numRoomID = Convert.ToInt32(RoomIDs[i]);
                //Rates
                bookingHeader.bitStandardRate = Convert.ToBoolean(StandardRateBits[i]);
                if (!Convert.ToBoolean(StandardRateBits[i]))
                {
                    bookingHeader.numRate = null;
                }
                else
                {
                    bookingHeader.numRate = Convert.ToDecimal(Rates[i]);
                }
                //Transport
                bookingHeader.bitTransportRequired = Convert.ToBoolean(TransportRequiredBits[i]);
                if (!Convert.ToBoolean(TransportRequiredBits[i]))
                {
                    bookingHeader.varFlightDetails = null;
                }
                else
                {
                    bookingHeader.varFlightDetails = FlightDetails[i];
                }
                //Lesson
                bookingHeader.bitKSLessonRequired = Convert.ToBoolean(LessonRequiredBits[i]);
                if (!Convert.ToBoolean(LessonRequiredBits[i]))
                {
                    bookingHeader.dtKsLessonRequiredFrom = null;
                    bookingHeader.dtKsLessonRequiredTo = null;
                }
                else
                {
                    bookingHeader.dtKsLessonRequiredFrom = Convert.ToDateTime(LessonRequiredsFrom[i]);
                    bookingHeader.dtKsLessonRequiredTo = Convert.ToDateTime(LessonRequiredsTo[i]);
                }
                //Rental
                bookingHeader.bitKSRentalRequired = Convert.ToBoolean(RentalRequiredBits[i]);
                if (!Convert.ToBoolean(RentalRequiredBits[i]))
                {
                    bookingHeader.dtKSRentalRequiredFrom = null;
                    bookingHeader.dtKSRentalRequiredTo = null;
                }
                else
                {
                    bookingHeader.dtKSRentalRequiredFrom = Convert.ToDateTime(RentalRequiredsFrom[i]);
                    bookingHeader.dtKSRentalRequiredTo = Convert.ToDateTime(RentalRequiredsTo[i]);
                }
                //Agent
                bookingHeader.bitAgentRequired = Convert.ToBoolean(AgentRequiredBits[i]);
                if (!Convert.ToBoolean(AgentRequiredBits[i]))
                {
                    bookingHeader.varAgentName = null;
                }
                else
                {
                    bookingHeader.varAgentName = AgentNames[i];
                }
                bookingHeader.varRemarks = Remarks[i];
                bookingHeader.numAdultsCount = Convert.ToInt32(AdultsCounts[i]);
                bookingHeader.numChildrensCount = Convert.ToInt32(ChildrensCounts[i]);
                bookingHeader.numInfantsCount = Convert.ToInt32(InfantsCounts[i]);
                bookingHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                bookingHeader.dtCreatedDate = System.DateTime.Now;
                db.BookingHeaders.Add(bookingHeader);
                db.SaveChanges();
            }

            var numBookingRefferenceID = Convert.ToInt32(varBookingRefferenceID[0]);

            BookingRefference bookingRefference = db.BookingRefferences.Find(numBookingRefferenceID);
            if (bookingRefference == null)
            {
                return HttpNotFound();
            }

            var BookingHeaders = db.BookingHeaders.Where(h => h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && h.bitActive != true && h.bitActive != false).OrderByDescending(h => h.numBookingHeaderID).ToList();
            foreach (var _header in BookingHeaders)
            {
                if (_header.bitActive != true)
                {
                    decimal numTotalCost = 0;
                    decimal numRate = 0;
                    decimal numPax = 0;

                    BookingHeader bookingHeader = db.BookingHeaders.Find(_header.numBookingHeaderID);
                    bookingHeader.bitActive = true;
                    bookingHeader.bitCheckedIn = false;
                    bookingHeader.bitCheckedOut = false;
                    db.Entry(bookingHeader).State = EntityState.Modified;
                    db.SaveChanges();

                    int numTotalDays = ((_header.dtToDate.Value.Date - _header.dtFromDate.Value.Date).Days);
                    numPax = Convert.ToDecimal(bookingHeader.numAdultsCount) + (Convert.ToDecimal(bookingHeader.numChildrensCount) * Convert.ToDecimal(0.5));

                    if (_header.bitStandardRate == false)
                    {
                        var numPaxCount = bookingHeader.numAdultsCount + bookingHeader.numChildrensCount + bookingHeader.numInfantsCount;
                        numRate = db.RoomTypeRates.Where(r => r.numRoomTypeID == _header.numRoomTypeID && r.numPersonCount == numPaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                        numTotalCost = (numRate * numPax);
                    }
                    else
                    {
                        numRate = _header.numRate.Value;
                        numTotalCost = (numRate * numPax);
                    }

                    BillHeader billHeader = new BillHeader();
                    billHeader.numBookingRefferenceID = bookingRefference.numBookingRefferenceID;
                    billHeader.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                    billHeader.numTotalCost = numTotalCost * numTotalDays;
                    billHeader.numPayedAmount = 0;
                    billHeader.numBalanceToPay = numTotalCost * numTotalDays;
                    billHeader.bitActive = true;
                    billHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    billHeader.dtCreatedDate = System.DateTime.Now;
                    db.BillHeaders.Add(billHeader);
                    db.SaveChanges();

                    BillDetail billDetail = new BillDetail();
                    billDetail.numBillHeaderID = billHeader.numBillHeaderID;
                    billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Room").Select(c => c.numChargeTypeID).FirstOrDefault();
                    billDetail.numRoomID = bookingHeader.numRoomID;
                    billDetail.numCost = numRate;
                    billDetail.numDuration = numTotalDays;
                    billDetail.numQuantity = numPax;
                    billDetail.numFinalCost = (numRate * numTotalDays) * numPax;
                    billDetail.bitActive = true;
                    billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    billDetail.dtCreatedDate = System.DateTime.Now;
                    db.BillDetails.Add(billDetail);
                    db.SaveChanges();
                }
            }
            TempData["BookingHeaderStatus"] = "Saved";
            return RedirectToAction("Index", new { varPage = "Booking" });
            //return RedirectToAction("AddBookingConfirmation", new { id = Convert.ToInt32(varBookingRefferenceID[0]) });
        }

        public ActionResult Bookings(int? id)
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
                ViewBag.numBookingRefferenceID = bookingRefference.numBookingRefferenceID;
                ViewBag.varBookingRefferenceNo = bookingRefference.varBookingRefferenceNo;

                var BookingHeaders = db.BookingHeaders.Where(h => h.numBookingRefferenceID == id && h.bitActive == true).OrderByDescending(h => h.numBookingHeaderID).ToList();
                ViewBag.BookingHeaders = BookingHeaders;
                var BookingIDs = BookingHeaders.Select(h => h.numBookingHeaderID).ToList();
                ViewBag.BookingDetails = db.BookingDetails.Where(b => b.bitActive == true && BookingIDs.Contains(b.numBookingHeaderID.Value)).OrderBy(x => x.dtCreatedDate).ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        public ActionResult DeleteAddBooking(int? id)
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

            var BookingHeaders = db.BookingHeaders.Where(h => h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID && h.bitActive != true && h.bitActive != false).ToList();
            if (BookingHeaders.Count != 0)
            {
                foreach (var BookingHeader in BookingHeaders)
                {
                    var BookingDetails = db.BookingDetails.Where(b => b.numBookingHeaderID == BookingHeader.numBookingHeaderID).ToList();
                    if (BookingDetails.Count != 0)
                    {
                        db.BookingDetails.RemoveRange(BookingDetails);
                        db.SaveChanges();
                    }
                }
                db.BookingHeaders.RemoveRange(BookingHeaders);
                db.SaveChanges();
            }
            db.BookingRefferences.Remove(bookingRefference);
            db.SaveChanges();

            TempData["BookingStatus"] = "Deleted";
            return RedirectToAction("Create");
        }

        public ActionResult BookingCalenderDetails(int? id)
        {
            if (Session["UserID"] != null)
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
                ViewBag.GuestCount = bookingHeader.numAdultsCount + bookingHeader.numChildrensCount + bookingHeader.numInfantsCount;
                ViewBag.BookingHeaders = bookingHeader;
                ViewBag.BookingDetails = db.BookingDetails.Where(d => d.bitActive == true && d.numBookingHeaderID == bookingHeader.numBookingHeaderID).OrderBy(x => x.dtCreatedDate).ToList();
                ViewBag.BillDetails = db.BillDetails.Where(b => b.BillHeader.numBookingHeaderID == bookingHeader.numBookingHeaderID && b.BillHeader.BookingRefference.bitActive == true && b.BillHeader.BookingRefference.bitClosed == false && b.BillHeader.bitActive == true && b.bitActive == true).OrderBy(x => x.dtCreatedDate).ToList();
                ViewBag.Receipts = db.Receipts.Where(r => r.bitActive == true && r.numBookingHeaderID == bookingHeader.numBookingHeaderID).OrderBy(x => x.dtCreatedDate).ToList();
                return PartialView("_BookingCalenderDetails", db.BillHeaders.Where(c => c.numBookingHeaderID == id).FirstOrDefault<BillHeader>());
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpGet]
        public ActionResult LoadBooking(int numBookingRefferenceID)
        {
            return Json(db.BookingHeaders.Include(b => b.Room).Include(b => b.RoomType).Where(r => r.numBookingRefferenceID == numBookingRefferenceID && r.bitActive == true && r.bitCheckedIn == true).Select(x => new { id = x.numBookingHeaderID, booking = x.RoomType.varRoomTypeName + " | Room: " + x.Room.varRoomNo, dtEndDate = x.dtToDate }).ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult CheckBookingExtend(int numBookingHeaderId, string extToDate)
        {
            var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
            extToDate = extToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;
            BookingHeader bookingHeader = db.BookingHeaders.Find(numBookingHeaderId);
            string extFromDate = bookingHeader.dtToDate.Value.ToString("yyyy-MM-dd") + " " + RoomcheckInouts[0].varRoomCheckInTime;
            var From = Convert.ToDateTime(extFromDate);
            var To = Convert.ToDateTime(extToDate);

            bool status = true;
            if (bookingHeader.dtFromDate.Value.Date == bookingHeader.dtCheckInDateTime.Value.Date)
            {
                var BookingIds = db.BookingHeaders.Where(b => b.bitActive == true && b.bitCheckedOut == false && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList();
                if (BookingIds.Contains(bookingHeader.numRoomID))
                {
                    status = false;
                }
            }
            else
            {
                var BookingIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtCheckInDateTime < To).Select(b => b.numRoomID).ToList();
                if (BookingIds.Contains(bookingHeader.numRoomID))
                {
                    status = false;
                }
            }
            return Json(new { status = status }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Edit(int? id)
        {
            if (Session["UserID"] != null)
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(id);
                if (bookingRefference == null)
                {
                    return HttpNotFound();
                }

                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                var BookingHeaders = db.BookingHeaders.Where(h => h.numBookingRefferenceID == bookingRefference.numBookingRefferenceID).OrderByDescending(h => h.numBookingHeaderID).ToList();
                List<ExtBooking> bookings = new List<ExtBooking>();
                int i = 1;
                foreach (var item in BookingHeaders)
                {
                    var numPaxCount = db.Rooms.Where(r => r.numRoomID == item.numRoomID && r.bitActive == true).Select(r => r.numMaxPaxAllowed.Value).FirstOrDefault();

                    bookings.Add(new ExtBooking
                    {
                        numIndex = i++,
                        numBookingID = item.numBookingHeaderID,
                        dtFromDate = item.dtFromDate.Value.ToString("yyyy-MM-dd") + " " + RoomcheckInouts[0].varRoomCheckInTime,
                        dtToDate = item.dtToDate.Value.ToString("yyyy-MM-dd") + " " + RoomcheckInouts[0].varRoomCheckOutTime,
                        numRoomTypeID = item.numRoomTypeID.Value,
                        numRoomID = item.numRoomID.Value,
                        numPaxCount = numPaxCount,
                        bitLessonRequired = item.bitKSLessonRequired.Value,
                        dtLessonFrom = item.dtKsLessonRequiredFrom == null ? "" : item.dtKsLessonRequiredFrom.Value.ToString("yyyy-MM-dd"),
                        dtLessonTo = item.dtKsLessonRequiredTo == null ? "" : item.dtKsLessonRequiredTo.Value.ToString("yyyy-MM-dd"),
                        bitRentalRequired = item.bitKSRentalRequired.Value,
                        dtRentalFrom = item.dtKSRentalRequiredFrom == null ? "" : item.dtKSRentalRequiredFrom.Value.ToString("yyyy-MM-dd"),
                        dtRentalTo = item.dtKSRentalRequiredTo == null ? "" : item.dtKSRentalRequiredTo.Value.ToString("yyyy-MM-dd"),
                        bitActive = item.bitActive.Value
                    });
                }

                TempData["numBookingRefferenceID"] = id;
                TempData["Bookings"] = bookings.ToList();
                return View(BookingHeaders);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpGet]
        public JsonResult LoadEditRoomCategories(int numBokingID, int numRoomTypeID, string dtFromDate, string dtToDate)
        {
            var From = Convert.ToDateTime(dtFromDate);
            var To = Convert.ToDateTime(dtToDate);

            List<int?> BookedRoomIds = new List<int?>();
            BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.numBookingHeaderID != numBokingID && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
            int extRoomTypeID = 7;// External Room Category
            return Json(new
            {
                rooms = db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.bitActive == true && r.numRoomTypeID != extRoomTypeID && r.numRoomTypeID != numRoomTypeID).OrderBy(x => x.varRoomName).Select(x => new { id = x.RoomType.numRoomTypeID, name = x.RoomType.varRoomTypeName }).Distinct().ToList(),
                roomId = numRoomTypeID,
                roomName = db.RoomTypes.Where(r => r.numRoomTypeID == numRoomTypeID).Select(r => r.varRoomTypeName).First()
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult LoadEditAvailableRooms(int numBokingID, int numRoomTypeID, int numRoomID, string dtFromDate, string dtToDate)
        {
            var From = Convert.ToDateTime(dtFromDate);
            var To = Convert.ToDateTime(dtToDate);

            List<int?> BookedRoomIds = new List<int?>();
            BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.numBookingHeaderID != numBokingID && b.dtToDate > From && b.dtFromDate < To).Select(b => b.numRoomID).ToList());
            var room = db.Rooms.Where(r => r.numRoomID == numRoomID).First();

            return Json(new
            {
                rooms = db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.numRoomTypeID == numRoomTypeID && r.bitActive == true && r.numRoomID != numRoomID).Select(x => new
                {
                    id = x.numRoomID,
                    name = "Room No: " + x.varRoomNo + " | Max Pax: " + x.numMaxPaxAllowed,
                    order = x.varRoomNo
                }).OrderBy(x => x.order).ToList(),
                roomId = room.numRoomID,
                roomName = "Room No: " + room.varRoomNo + " | Max Pax: " + room.numMaxPaxAllowed
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult LoadNonBookingRoomCategories(int id, string dtFromDate, string dtToDate, string Bookings, string RemovedBookings)
        {
            var js = new JavaScriptSerializer();
            List<int> NonBookings = new List<int>();
            var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
            dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
            dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

            var From = Convert.ToDateTime(dtFromDate);
            var To = Convert.ToDateTime(dtToDate);

            List<int?> BookedRoomIds = new List<int?>();
            if (Bookings == "[]")
            {
                if (RemovedBookings != "[]")
                {
                    NonBookings.AddRange(js.Deserialize<List<int>>(RemovedBookings));
                    BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList();
                }
                else
                {
                    BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id).Select(b => b.numRoomID).ToList();
                }
            }
            else
            {
                List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);

                if (RemovedBookings != "[]")
                {
                    NonBookings.AddRange(js.Deserialize<List<int>>(RemovedBookings));

                    BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList());
                    BookedRoomIds.AddRange(bookings.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingID != id).Select(b => b.numRoomID).ToList());
                }
                else
                {
                    BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id).Select(b => b.numRoomID).ToList());
                    BookedRoomIds.AddRange(bookings.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingID != id).Select(b => b.numRoomID).ToList());
                }
            }

            int extRoomTypeID = 7;// External Room Category
            return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.bitActive == true && r.numRoomTypeID != extRoomTypeID).OrderBy(x => x.varRoomName).Select(x => new { id = x.RoomType.numRoomTypeID, name = x.RoomType.varRoomTypeName }).Distinct().ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult LoadNonBookingAvailableRooms(int id, int numRoomTypeID, string dtFromDate, string dtToDate, string Bookings, string RemovedBookings)
        {
            var js = new JavaScriptSerializer();
            List<int> NonBookings = new List<int>();
            var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
            dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
            dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

            var From = Convert.ToDateTime(dtFromDate);
            var To = Convert.ToDateTime(dtToDate);

            List<int?> BookedRoomIds = new List<int?>();
            if (Bookings == "[]")
            {
                if (RemovedBookings != "[]")
                {
                    NonBookings.AddRange(js.Deserialize<List<int>>(RemovedBookings));
                    BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList();
                }
                else
                {
                    BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id).Select(b => b.numRoomID).ToList();
                }
            }
            else
            {
                List<Booking> bookings = js.Deserialize<List<Booking>>(Bookings);
                if (RemovedBookings != "[]")
                {
                    NonBookings.AddRange(js.Deserialize<List<int>>(RemovedBookings));

                    BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id && !NonBookings.Contains(b.numBookingHeaderID)).Select(b => b.numRoomID).ToList());
                    BookedRoomIds.AddRange(bookings.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingID != id).Select(b => b.numRoomID).ToList());
                }
                else
                {
                    BookedRoomIds.AddRange(db.BookingHeaders.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingHeaderID != id).Select(b => b.numRoomID).ToList());
                    BookedRoomIds.AddRange(bookings.Where(b => b.bitActive == true && b.dtToDate > From && b.dtFromDate < To && b.numBookingID != id).Select(b => b.numRoomID).ToList());
                }
            }
            return Json(db.Rooms.Where(r => !BookedRoomIds.Contains(r.numRoomID) && r.numRoomTypeID == numRoomTypeID && r.bitActive == true).Select(x => new
            {
                id = x.numRoomID,
                name = "Room No: " + x.varRoomNo + " | Max Pax: " + x.numMaxPaxAllowed,
                order = x.varRoomNo
            }).OrderBy(x => x.order).ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(FormCollection formCollection)
        {
            decimal numTotalCost = 0;
            decimal numRate = 0;
            decimal numPax = 0;

            var numBookingRefferenceID = formCollection.GetValues("numBookingRefferenceID");
            int numRefID = Convert.ToInt32(numBookingRefferenceID[0]);

            List<BookingHeader> _bookingHeaders = db.BookingHeaders.Where(b => b.bitActive == true && b.numBookingRefferenceID == numRefID).ToList();
            var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();

            var numBookingHeaderID = formCollection.GetValues("numBookingHeaderID");
            var FromDates = formCollection.GetValues("dtFromDate");
            var ToDates = formCollection.GetValues("dtToDate");
            var RoomTypeIDs = formCollection.GetValues("numRoomTypeID");
            var RoomIDs = formCollection.GetValues("numRoomID");
            var StandardRateBits = formCollection.GetValues("bitStandardRateValue");
            var Rates = formCollection.GetValues("numRate");
            var TransportRequiredBits = formCollection.GetValues("bitTransportRequiredValue");
            var FlightDetails = formCollection.GetValues("varFlightDetails");
            var LessonRequiredBits = formCollection.GetValues("bitKSLessonRequiredValue");
            var LessonRequiredsFrom = formCollection.GetValues("dtKsLessonRequiredFrom");
            var LessonRequiredsTo = formCollection.GetValues("dtKsLessonRequiredTo");
            var RentalRequiredBits = formCollection.GetValues("bitKSRentalRequiredValue");
            var RentalRequiredsFrom = formCollection.GetValues("dtKSRentalRequiredFrom");
            var RentalRequiredsTo = formCollection.GetValues("dtKSRentalRequiredTo");
            var AgentRequiredBits = formCollection.GetValues("bitKSAgentRequiredValue");
            var AgentNames = formCollection.GetValues("varAgentName");
            var Remarks = formCollection.GetValues("varRemarks");
            var AdultsCounts = formCollection.GetValues("numAdultsCount");
            var ChildrensCounts = formCollection.GetValues("numChildrensCount");
            var InfantsCounts = formCollection.GetValues("numInfantsCount");

            var varBookingPersonName = formCollection.GetValues("item.varBookingPersonName");
            var varBookingPersonEmail = formCollection.GetValues("item.varBookingPersonEmail");

            for (int i = numBookingHeaderID.Count() - 1; i >= 0; i--)
            {
                if (numBookingHeaderID[i] != "-1")
                {
                    int id = Convert.ToInt32(numBookingHeaderID[i]);
                    BookingHeader bookingHeader = db.BookingHeaders.Find(id);
                    bookingHeader.numBookingRefferenceID = numRefID;
                    bookingHeader.dtFromDate = Convert.ToDateTime(FromDates[i] + " " + RoomcheckInouts[0].varRoomCheckInTime);
                    bookingHeader.dtToDate = Convert.ToDateTime(ToDates[i] + " " + RoomcheckInouts[0].varRoomCheckOutTime);
                    bookingHeader.numRoomTypeID = Convert.ToInt32(RoomTypeIDs[i]);
                    bookingHeader.numRoomID = Convert.ToInt32(RoomIDs[i]);
                    //Rates
                    bookingHeader.bitStandardRate = Convert.ToBoolean(StandardRateBits[i]);
                    if (!Convert.ToBoolean(StandardRateBits[i]))
                    {
                        bookingHeader.numRate = null;
                    }
                    else
                    {
                        bookingHeader.numRate = Convert.ToDecimal(Rates[i]);
                    }
                    //Transport
                    bookingHeader.bitTransportRequired = Convert.ToBoolean(TransportRequiredBits[i]);
                    if (!Convert.ToBoolean(TransportRequiredBits[i]))
                    {
                        bookingHeader.varFlightDetails = null;
                    }
                    else
                    {
                        bookingHeader.varFlightDetails = FlightDetails[i];
                    }
                    //Lesson
                    bookingHeader.bitKSLessonRequired = Convert.ToBoolean(LessonRequiredBits[i]);
                    if (!Convert.ToBoolean(LessonRequiredBits[i]))
                    {
                        bookingHeader.dtKsLessonRequiredFrom = null;
                        bookingHeader.dtKsLessonRequiredTo = null;
                    }
                    else
                    {
                        bookingHeader.dtKsLessonRequiredFrom = Convert.ToDateTime(LessonRequiredsFrom[i]);
                        bookingHeader.dtKsLessonRequiredTo = Convert.ToDateTime(LessonRequiredsTo[i]);
                    }
                    //Rental
                    bookingHeader.bitKSRentalRequired = Convert.ToBoolean(RentalRequiredBits[i]);
                    if (!Convert.ToBoolean(RentalRequiredBits[i]))
                    {
                        bookingHeader.dtKSRentalRequiredFrom = null;
                        bookingHeader.dtKSRentalRequiredTo = null;
                    }
                    else
                    {
                        bookingHeader.dtKSRentalRequiredFrom = Convert.ToDateTime(RentalRequiredsFrom[i]);
                        bookingHeader.dtKSRentalRequiredTo = Convert.ToDateTime(RentalRequiredsTo[i]);
                    }
                    //Agent
                    bookingHeader.bitAgentRequired = Convert.ToBoolean(AgentRequiredBits[i]);
                    if (!Convert.ToBoolean(AgentRequiredBits[i]))
                    {
                        bookingHeader.varAgentName = null;
                    }
                    else
                    {
                        bookingHeader.varAgentName = AgentNames[i];
                    }
                    bookingHeader.varRemarks = Remarks[i];
                    bookingHeader.varBookingPersonName = varBookingPersonName[0];
                    bookingHeader.varBookingPersonEmail = varBookingPersonEmail[0];
                    bookingHeader.numAdultsCount = Convert.ToInt32(AdultsCounts[i]);
                    bookingHeader.numChildrensCount = Convert.ToInt32(ChildrensCounts[i]);
                    bookingHeader.numInfantsCount = Convert.ToInt32(InfantsCounts[i]);
                    bookingHeader.bitActive = true;
                    bookingHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                    bookingHeader.dtUpdatetDate = System.DateTime.Now;
                    db.Entry(bookingHeader).State = EntityState.Modified;
                    db.SaveChanges();

                    int numTotalDays = ((bookingHeader.dtToDate.Value.Date - bookingHeader.dtFromDate.Value.Date).Days);
                    numPax = Convert.ToDecimal(bookingHeader.numAdultsCount) + (Convert.ToDecimal(bookingHeader.numChildrensCount) * Convert.ToDecimal(0.5));


                    if (bookingHeader.bitStandardRate == false)
                    {
                        var numPaxCount = bookingHeader.numAdultsCount + bookingHeader.numChildrensCount + bookingHeader.numInfantsCount;
                        numRate = db.RoomTypeRates.Where(r => r.numRoomTypeID == bookingHeader.numRoomTypeID && r.numPersonCount == numPaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                        numTotalCost = (numRate * numPax);
                    }
                    else
                    {
                        numRate = bookingHeader.numRate.Value;
                        numTotalCost = (numRate * numPax);
                    }

                    var BillHeader = db.BillHeaders.Where(b => b.numBookingRefferenceID == numRefID && b.numBookingHeaderID == bookingHeader.numBookingHeaderID && b.bitActive == true).First();

                    var numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Room").Select(c => c.numChargeTypeID).FirstOrDefault();
                    var BillDetail = db.BillDetails.Where(b => b.numBillHeaderID == BillHeader.numBillHeaderID && b.numChargeTypeID == numChargeTypeID && b.bitActive == true).First();
                    BillDetail.numBillHeaderID = BillHeader.numBillHeaderID;
                    BillDetail.numChargeTypeID = numChargeTypeID;
                    BillDetail.numRoomID = bookingHeader.numRoomID;
                    BillDetail.numCost = numRate;
                    BillDetail.numDuration = numTotalDays;
                    BillDetail.numQuantity = numPax;
                    BillDetail.numFinalCost = (numRate * numTotalDays) * numPax;
                    BillDetail.bitActive = true;
                    BillDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                    BillDetail.dtUpdatetDate = System.DateTime.Now;
                    db.Entry(BillDetail).State = EntityState.Modified;
                    db.SaveChanges();

                    BillHeader.numBookingRefferenceID = numRefID;
                    BillHeader.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                    BillHeader.numTotalCost = numTotalCost * numTotalDays;
                    
                    // Advance payments may already exist. So it must be reflected.
                    // BillHeader.numPayedAmount = 0;
                    BillHeader.numBalanceToPay = numTotalCost * numTotalDays - BillHeader.numPayedAmount;
                    
                    BillHeader.bitActive = true;
                    BillHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                    BillHeader.dtUpdatetDate = System.DateTime.Now;
                    db.Entry(BillHeader).State = EntityState.Modified;
                    db.SaveChanges();

                    _bookingHeaders.Remove(bookingHeader);
                }
                else
                {
                    BookingHeader bookingHeader = new BookingHeader();
                    bookingHeader.numBookingRefferenceID = numRefID;
                    bookingHeader.dtFromDate = Convert.ToDateTime(FromDates[i] + " " + RoomcheckInouts[0].varRoomCheckInTime);
                    bookingHeader.dtToDate = Convert.ToDateTime(ToDates[i] + " " + RoomcheckInouts[0].varRoomCheckOutTime);
                    bookingHeader.numRoomTypeID = Convert.ToInt32(RoomTypeIDs[i]);
                    bookingHeader.numRoomID = Convert.ToInt32(RoomIDs[i]);
                    //Rates
                    bookingHeader.bitStandardRate = Convert.ToBoolean(StandardRateBits[i]);
                    if (!Convert.ToBoolean(StandardRateBits[i]))
                    {
                        bookingHeader.numRate = null;
                    }
                    else
                    {
                        bookingHeader.numRate = Convert.ToDecimal(Rates[i]);
                    }
                    //Transport
                    bookingHeader.bitTransportRequired = Convert.ToBoolean(TransportRequiredBits[i]);
                    if (!Convert.ToBoolean(TransportRequiredBits[i]))
                    {
                        bookingHeader.varFlightDetails = null;
                    }
                    else
                    {
                        bookingHeader.varFlightDetails = FlightDetails[i];
                    }
                    //Lesson
                    bookingHeader.bitKSLessonRequired = Convert.ToBoolean(LessonRequiredBits[i]);
                    if (!Convert.ToBoolean(LessonRequiredBits[i]))
                    {
                        bookingHeader.dtKsLessonRequiredFrom = null;
                        bookingHeader.dtKsLessonRequiredTo = null;
                    }
                    else
                    {
                        bookingHeader.dtKsLessonRequiredFrom = Convert.ToDateTime(LessonRequiredsFrom[i]);
                        bookingHeader.dtKsLessonRequiredTo = Convert.ToDateTime(LessonRequiredsTo[i]);
                    }
                    //Rental
                    bookingHeader.bitKSRentalRequired = Convert.ToBoolean(RentalRequiredBits[i]);
                    if (!Convert.ToBoolean(RentalRequiredBits[i]))
                    {
                        bookingHeader.dtKSRentalRequiredFrom = null;
                        bookingHeader.dtKSRentalRequiredTo = null;
                    }
                    else
                    {
                        bookingHeader.dtKSRentalRequiredFrom = Convert.ToDateTime(RentalRequiredsFrom[i]);
                        bookingHeader.dtKSRentalRequiredTo = Convert.ToDateTime(RentalRequiredsTo[i]);
                    }
                    //Agent
                    bookingHeader.bitAgentRequired = Convert.ToBoolean(AgentRequiredBits[i]);
                    if (!Convert.ToBoolean(AgentRequiredBits[i]))
                    {
                        bookingHeader.varAgentName = null;
                    }
                    else
                    {
                        bookingHeader.varAgentName = AgentNames[i];
                    }
                    bookingHeader.varRemarks = Remarks[i];
                    bookingHeader.varBookingPersonName = varBookingPersonName[0];
                    bookingHeader.varBookingPersonEmail = varBookingPersonEmail[0];
                    bookingHeader.numAdultsCount = Convert.ToInt32(AdultsCounts[i]);
                    bookingHeader.numChildrensCount = Convert.ToInt32(ChildrensCounts[i]);
                    bookingHeader.numInfantsCount = Convert.ToInt32(InfantsCounts[i]);
                    bookingHeader.bitActive = true;
                    bookingHeader.bitCheckedIn = false;
                    bookingHeader.bitCheckedOut = false;
                    bookingHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    bookingHeader.dtCreatedDate = System.DateTime.Now;
                    db.BookingHeaders.Add(bookingHeader);
                    db.SaveChanges();

                    int numTotalDays = ((bookingHeader.dtToDate.Value.Date - bookingHeader.dtFromDate.Value.Date).Days);
                    numPax = Convert.ToDecimal(bookingHeader.numAdultsCount) + (Convert.ToDecimal(bookingHeader.numChildrensCount) * Convert.ToDecimal(0.5));

                    if (bookingHeader.bitStandardRate == false)
                    {
                        var numPaxCount = bookingHeader.numAdultsCount + bookingHeader.numChildrensCount + bookingHeader.numInfantsCount;
                        numRate = db.RoomTypeRates.Where(r => r.numRoomTypeID == bookingHeader.numRoomTypeID && r.numPersonCount == numPaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                        numTotalCost = numRate * numPax;
                    }
                    else
                    {
                        numRate = bookingHeader.numRate.Value;
                        numTotalCost = numRate * numPax;
                    }

                    BillHeader billHeader = new BillHeader();
                    billHeader.numBookingRefferenceID = numRefID;
                    billHeader.numBookingHeaderID = bookingHeader.numBookingHeaderID;
                    billHeader.numTotalCost = numTotalCost * numTotalDays;
                    billHeader.numPayedAmount = 0;
                    billHeader.numBalanceToPay = numTotalCost * numTotalDays;
                    billHeader.bitActive = true;
                    billHeader.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    billHeader.dtCreatedDate = System.DateTime.Now;
                    db.BillHeaders.Add(billHeader);
                    db.SaveChanges();

                    BillDetail billDetail = new BillDetail();
                    billDetail.numBillHeaderID = billHeader.numBillHeaderID;
                    billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Room").Select(c => c.numChargeTypeID).FirstOrDefault();
                    billDetail.numRoomID = bookingHeader.numRoomID;
                    billDetail.numCost = numRate;
                    billDetail.numDuration = numTotalDays;
                    billDetail.numQuantity = numPax;
                    billDetail.numFinalCost = (numRate * numTotalDays) * numPax;
                    billDetail.bitActive = true;
                    billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
                    billDetail.dtCreatedDate = System.DateTime.Now;
                    db.BillDetails.Add(billDetail);
                    db.SaveChanges();
                }
            }

            // Removes all existing bills for this booking refference
            // Any single room advance payments should be changed to reference level advance payments
            if (_bookingHeaders.Count != 0)
            {
                foreach (var item in _bookingHeaders)
                {
                    List<Receipt> receipts = db.Receipts.Where(r => r.bitActive == true && r.bitSingleBooking == true && r.numBookingHeaderID == item.numBookingHeaderID).ToList();
                    foreach (Receipt receipt in receipts) {
                        receipt.bitSingleBooking = false;
                        receipt.numBookingHeaderID = null;
                        receipt.numBillHeaderID = null;
                        receipt.bitActive = true;
                        receipt.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                        receipt.dtUpdatetDate = System.DateTime.Now;
                        db.Entry(receipt).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    var BillHeader = db.BillHeaders.Where(bh => bh.numBookingRefferenceID == numRefID && bh.numBookingHeaderID == item.numBookingHeaderID).First();
                    var BillDetails = db.BillDetails.Where(bd => bd.numBillHeaderID == BillHeader.numBillHeaderID).ToList();
                    if (BillDetails.Count != 0)
                    {
                        db.BillDetails.RemoveRange(BillDetails);
                        db.SaveChanges();
                    }

                    db.BillHeaders.Remove(BillHeader);
                    db.SaveChanges();

                    db.BookingHeaders.Remove(item);
                    db.SaveChanges();
                }
            }

            // if the booking persons name is changed the refference number is changed (?)
            BookingRefference bookingRefference = db.BookingRefferences.Find(numRefID);

            var bookingPersonName = varBookingPersonName[0].Replace(" ", "").ToUpper().PadLeft(10, '#').Substring(0, 10);
            var numBookingNO = Convert.ToInt32(bookingRefference.varBookingRefferenceNo.Split('/')[2]);
            string varRef = "KSL/INT/" + numBookingNO.ToString("00000") + "/" + bookingPersonName;

            bookingRefference.varBookingRefferenceNo = varRef;
            bookingRefference.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
            bookingRefference.dtUpdatetDate = System.DateTime.Now;
            db.Entry(bookingRefference).State = EntityState.Modified;
            db.SaveChanges();

            TempData["BookingHeaderStatus"] = "Saved";
            return RedirectToAction("Index", new { varPage = "Booking" });
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