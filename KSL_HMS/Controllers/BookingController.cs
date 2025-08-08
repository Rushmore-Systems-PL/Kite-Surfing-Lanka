using KSL_HMS.DB;
using KSL_HMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace KSL_HMS.Controllers
{
    public class BookingController : Controller
    {
        private readonly ConnectionHMSDB db = new ConnectionHMSDB();

        public class BookingAvailability
        {
            public int numRoomtypeID { get; set; }
            public string varRoomtypeName { get; set; }
            public string varRoomDescription { get; set; }
            public int numAvailableCount { get; set; }
            public decimal numRatePerRoom { get; set; }
            public decimal numRatePerPeriod { get; set; }
            public int numPax { get; set; }
            public int numTotalDays { get; set; }
            public string varGuestText { get; set; }
        }

        public class RoomsGuests
        {
            public int numID { get; set; }
            public int numRoomsCount { get; set; }
            public int numAdultsCount { get; set; } = 0;
            public int numChildrenCount { get; set; } = 0;
            public int numInfantsCount { get; set; } = 0;
        }

        public ActionResult Index()
        //public ActionResult Index(DateTime dtStDate, DateTime dtEnDate, int numRoomsCount, int numPaxCount)
        {
            TempData["dtFromDate"] = System.DateTime.Now.ToString("yyyy-MM-dd");
            TempData["dtToDate"] = System.DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");

            RoomsGuests roomsGuests = new RoomsGuests();
            roomsGuests.numID = 0; //ID should start from 0
            roomsGuests.numRoomsCount = 1;
            roomsGuests.numAdultsCount = 2;
            //roomsGuests.numChildrenCount = 1;

            string varRoomsGuestText = string.Empty;
            if (roomsGuests.numRoomsCount == 1)
            {
                varRoomsGuestText += roomsGuests.numRoomsCount.ToString("00") + " Unit";
            }
            else
            {
                varRoomsGuestText += roomsGuests.numRoomsCount.ToString("00") + " Units";
            }

            if (roomsGuests.numAdultsCount == 1)
            {
                varRoomsGuestText += ", " + roomsGuests.numAdultsCount.ToString("00") + " Adult";
            }
            else if (roomsGuests.numAdultsCount != 0)
            {
                varRoomsGuestText += ", " + roomsGuests.numAdultsCount.ToString("00") + " Adults";
            }

            if (roomsGuests.numChildrenCount == 1)
            {
                varRoomsGuestText += ", " + roomsGuests.numChildrenCount.ToString("00") + " Child";
            }
            else if (roomsGuests.numChildrenCount != 0)
            {
                varRoomsGuestText += ", " + roomsGuests.numChildrenCount.ToString("00") + " Children";
            }

            if (roomsGuests.numInfantsCount == 1)
            {
                varRoomsGuestText += ", " + roomsGuests.numInfantsCount.ToString("00") + " Infant";
            }
            else if (roomsGuests.numInfantsCount != 0)
            {
                varRoomsGuestText += ", " + roomsGuests.numInfantsCount.ToString("00") + " Infants";
            }

            ViewBag.varRoomsGuestText = varRoomsGuestText;
            ViewBag.RoomsGuests = roomsGuests;
            return View();
        }

        [HttpGet]
        public JsonResult loadPeriodAvailbleRooms(string dtFromDate, string dtToDate, string RoomsGuests)
        {
            var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
            dtFromDate = dtFromDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
            dtToDate = dtToDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

            DateTime dtStDate = Convert.ToDateTime(dtFromDate);
            DateTime dtEnDate = Convert.ToDateTime(dtToDate);

            var js = new JavaScriptSerializer();
            RoomsGuests NonBookings = new RoomsGuests();
            NonBookings.numID = js.Deserialize<RoomsGuests>(RoomsGuests).numID;
            NonBookings.numRoomsCount = js.Deserialize<RoomsGuests>(RoomsGuests).numRoomsCount;
            NonBookings.numAdultsCount = js.Deserialize<RoomsGuests>(RoomsGuests).numAdultsCount;
            NonBookings.numChildrenCount = js.Deserialize<RoomsGuests>(RoomsGuests).numChildrenCount;
            NonBookings.numInfantsCount = js.Deserialize<RoomsGuests>(RoomsGuests).numInfantsCount;

            int numTotalDays = ((dtEnDate.Date - dtStDate.Date).Days);
            List<int> _NonValidRooms = new List<int> { 7, 8 }; //External, Hired Room
            int numRoomCount = 0;
            int numPaxAccomodate = 0;
            string varRoomsGuestText = string.Empty;
            var BookedRoomIds = db.BookingHeaders.Where(b => b.bitActive == true && DbFunctions.TruncateTime(b.dtToDate) >= DbFunctions.TruncateTime(dtStDate) && DbFunctions.TruncateTime(b.dtFromDate) <= DbFunctions.TruncateTime(dtEnDate)).Select(b => b.numRoomID).ToList();
            List<BookingAvailability> bookingAvailabilities = new List<BookingAvailability>();

            int numPaxCount = NonBookings.numAdultsCount + NonBookings.numChildrenCount + NonBookings.numInfantsCount;
            double numRatePaxCount = Convert.ToDouble(NonBookings.numAdultsCount) + (Convert.ToDouble(NonBookings.numChildrenCount) * 0.5);

            if (NonBookings.numAdultsCount == 1)
            {
                varRoomsGuestText += NonBookings.numAdultsCount.ToString("00") + " Adult, ";
            }
            else if (NonBookings.numAdultsCount != 0)
            {
                varRoomsGuestText += NonBookings.numAdultsCount.ToString("00") + " Adults, ";
            }

            if (NonBookings.numChildrenCount == 1)
            {
                varRoomsGuestText += NonBookings.numChildrenCount.ToString("00") + " Child";
            }
            else if (NonBookings.numChildrenCount != 0)
            {
                varRoomsGuestText += NonBookings.numChildrenCount.ToString("00") + " Children";
            }
            else
            {
                if (varRoomsGuestText != string.Empty)
                {
                    varRoomsGuestText = varRoomsGuestText.Substring(0, varRoomsGuestText.Length - 2);
                }
            }

            if (NonBookings.numInfantsCount == 1)
            {
                varRoomsGuestText += ", " + NonBookings.numInfantsCount.ToString("00") + " Infant";
            }
            else if (NonBookings.numInfantsCount != 0)
            {
                varRoomsGuestText += ", " + NonBookings.numInfantsCount.ToString("00") + " Infants";
            }

            var rooms = db.Rooms.Include(r => r.RoomType).Where(r => r.bitActive == true && r.numMaxPaxAllowed >= numPaxCount && !BookedRoomIds.Contains(r.numRoomID) && !_NonValidRooms.Contains(r.numRoomTypeID.Value)).OrderBy(r => r.numRoomTypeID).ToList();
            foreach (var room in rooms)
            {
                var BookingRoomTypeAvailabilityID = bookingAvailabilities.Select(b => b.numRoomtypeID).ToList();
                if (!BookingRoomTypeAvailabilityID.Contains(room.numRoomTypeID.Value))
                {
                    decimal numRate = db.RoomTypeRates.Where(r => r.numRoomTypeID == room.RoomType.numRoomTypeID && r.numPersonCount >= numRatePaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault();
                    var numPaxAllowedCount = rooms.Where(r => r.numRoomTypeID == room.numRoomTypeID).Sum(r => r.numMaxPaxAllowed.Value);
                    var numroomTypeCount = rooms.Where(r => r.numRoomTypeID == room.numRoomTypeID).Count();
                    numPaxAccomodate = numPaxAccomodate + numPaxAllowedCount;

                    if (numroomTypeCount >= 1 && numPaxAllowedCount >= numPaxCount)
                    {
                        bookingAvailabilities.Add(new BookingAvailability
                        {
                            numRoomtypeID = room.RoomType.numRoomTypeID,
                            varRoomtypeName = room.RoomType.varRoomTypeName,
                            varRoomDescription = room.RoomType.varRoomTypeDescription,
                            numAvailableCount = numroomTypeCount,
                            numRatePerPeriod = (numRate * Convert.ToDecimal(numRatePaxCount)) * numTotalDays,
                            numRatePerRoom = db.RoomTypeRates.Where(r => r.numRoomTypeID == room.RoomType.numRoomTypeID && r.numPersonCount >= numRatePaxCount).Select(r => r.numRatePerPerson.Value).FirstOrDefault(),
                            numPax = numPaxCount,
                            numTotalDays = numTotalDays,
                            varGuestText = varRoomsGuestText
                        });
                        numRoomCount = numRoomCount + numroomTypeCount;
                    }
                }
            }

            if (NonBookings.numRoomsCount == 1)
            {
                string viewContent = ConvertViewToString("_LoadSingleUnit", bookingAvailabilities);
                return Json(new
                {
                    varBookingType = "Single",
                    PartialView = viewContent,
                    numRoomCount = numRoomCount.ToString("00"),
                    dtCheckIn = dtStDate.ToString("ddd dd MMM yyyy"),
                    dtCheckOut = dtEnDate.ToString("ddd dd MMM yyyy"),
                    CheckInTime = dtStDate.ToString("hh:mm"),
                    CheckOutTime = dtEnDate.ToString("hh:mm"),
                    numTotalDays = numTotalDays.ToString("00"),
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                ViewBag.SelectedBooking = NonBookings;
                string viewContent = ConvertViewToString("_loadMultipleUnits", bookingAvailabilities);
                return Json(new
                {
                    varBookingType = "Multiple",
                    PartialView = viewContent,
                    numRoomCount = numRoomCount.ToString("00"),
                    dtCheckIn = dtStDate.ToString("ddd dd MMM yyyy"),
                    dtCheckOut = dtEnDate.ToString("ddd dd MMM yyyy"),
                    CheckInTime = dtStDate.ToString("hh:mm"),
                    CheckOutTime = dtEnDate.ToString("hh:mm"),
                    numTotalDays = numTotalDays.ToString("00"),
                }, JsonRequestBehavior.AllowGet);
            }
        }

        //public ActionResult CreateBooking(string dtStDate, string dtEnDate, int numRoomsCount, int numPaxCount, string Bookings)
        //{
        //    var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
        //    dtStDate = dtStDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
        //    dtEnDate = dtEnDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;
        //    DateTime dtFromDate = Convert.ToDateTime(dtStDate);
        //    DateTime dtToDate = Convert.ToDateTime(dtEnDate);

        //    var js = new JavaScriptSerializer();
        //    List<SelectedRoomBooking> selectedRoomBookings = js.Deserialize<List<SelectedRoomBooking>>(Bookings);

        //    TempData["StartDateToEndDate1"] = dtFromDate.ToString("yyyy-MM-dd") + " to " + dtToDate.ToString("yyyy-MM-dd");
        //    TempData["NoOfRoom1"] = numRoomsCount.ToString("00");
        //    TempData["NoofPeople1"] = numPaxCount.ToString("00");
        //    return View(selectedRoomBookings);
        //}

        private string ConvertViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (StringWriter writer = new StringWriter())
            {
                ViewEngineResult vResult = ViewEngines.Engines.FindPartialView(ControllerContext, viewName);
                ViewContext vContext = new ViewContext(this.ControllerContext, vResult.View, ViewData, new TempDataDictionary(), writer);
                vResult.View.Render(vContext, writer);
                return writer.ToString();
            }
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