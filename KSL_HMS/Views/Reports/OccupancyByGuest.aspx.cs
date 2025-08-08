using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

namespace KSL_HMS.Views.Reports
{
    public partial class OccupancyByGuest : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class OccupancyByGuests
        {
            public string varRoomType { get; set; }
            public double numTotalAvailable { get; set; }
            public double numTotalPax { get; set; }
            public double numOccupancy { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Session["dtStart"].ToString();
                var EDate = Session["dtEnd"].ToString();

                var RoomcheckInouts = db.RoomCheckInOuts.Where(r => r.bitActive == true).ToList();
                SDate = SDate + " " + RoomcheckInouts[0].varRoomCheckInTime;
                EDate = EDate + " " + RoomcheckInouts[0].varRoomCheckOutTime;

                var dtFromDate = Convert.ToDateTime(SDate);
                var dtToDate = Convert.ToDateTime(EDate);
                double numTotalDays = (dtToDate - dtFromDate).TotalDays;

                var RoomTypes = db.RoomTypes.Where(r => r.bitActive == true && r.varRoomTypeName != "External" && r.varRoomTypeName != "Hired Room").OrderBy(r => r.varRoomTypeName).ToList();
                List<Room> _rooms = db.Rooms.Where(r => r.bitActive == true).ToList();
                List<BookingHeader> _bookingHeaders = db.BookingHeaders.Where(b => b.bitActive == true && DbFunctions.TruncateTime(b.dtToDate) >= DbFunctions.TruncateTime(dtFromDate) && DbFunctions.TruncateTime(b.dtFromDate) <= DbFunctions.TruncateTime(dtToDate)).ToList();
                List<BookingDetail> _bookingDetails = db.BookingDetails.Where(b => b.bitActive == true).ToList();
                List<OccupancyByGuests> _occupancyByGuests = new List<OccupancyByGuests>();

                foreach (var roomType in RoomTypes)
                {
                    var Bookings = _bookingHeaders.Where(b => b.numRoomTypeID == roomType.numRoomTypeID).ToList();
                    var numPaxCount = _rooms.Where(r => r.numRoomTypeID == roomType.numRoomTypeID).Sum(r => r.numMaxPaxAllowed);

                    double numTotalPax = 0;
                    foreach (var booking in Bookings)
                    {
                        double numpaxBooked = 0;
                        double numTotalRoomsBooked = 0;
                        if (booking.dtToDate >= dtFromDate && booking.dtFromDate < dtFromDate)
                        {
                            numTotalRoomsBooked = (booking.dtToDate.Value.Date - dtFromDate).TotalDays;
                        }
                        else if (booking.dtFromDate <= dtToDate && booking.dtToDate > dtToDate)
                        {
                            numTotalRoomsBooked = (dtToDate - booking.dtFromDate.Value.Date).TotalDays;
                        }
                        else
                        {
                            numTotalRoomsBooked = (booking.dtToDate.Value.Date - booking.dtFromDate.Value.Date).TotalDays;
                        }
                        numpaxBooked = _bookingDetails.Where(b => b.numBookingHeaderID == booking.numBookingHeaderID).Count();
                        numTotalPax = numTotalPax + (numTotalRoomsBooked * numpaxBooked);
                    }

                    _occupancyByGuests.Add(new OccupancyByGuests
                    {
                        varRoomType = roomType.varRoomTypeName,
                        numTotalAvailable = numPaxCount.Value * numTotalDays,
                        numTotalPax = numTotalPax,
                        numOccupancy = numTotalPax / (numPaxCount.Value * numTotalDays) * 100
                    });
                }

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/OccupancyByGuest.rdlc");
                ReportDataSource datasource = new ReportDataSource();
                datasource.Name = "OccupancyByGuestsDataSet";
                datasource.Value = _occupancyByGuests;
                ReportViewer1.LocalReport.DataSources.Add(datasource);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Occupancy Rate By Rooms";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("RepoStDate", dtFromDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", dtToDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}