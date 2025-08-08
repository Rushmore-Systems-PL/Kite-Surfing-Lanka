using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

namespace KSL_HMS.Views.Reports
{
    public partial class OccupancyByRoom : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class OccupancyByRooms
        {
            public string varRoomType { get; set; }
            public double numTotalRoomsAvailable { get; set; }
            public double numTotalRoomsBooked { get; set; }
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
                List<OccupancyByRooms> _occupancyByRooms = new List<OccupancyByRooms>();

                foreach (var roomType in RoomTypes)
                {
                    var numTotalRoomsAvailable = (_rooms.Where(r => r.numRoomTypeID == roomType.numRoomTypeID).Count());
                    var Bookings = _bookingHeaders.Where(b => b.numRoomTypeID == roomType.numRoomTypeID).ToList();

                    double numTotalRoomsBooked = 0;
                    foreach (var booking in Bookings)
                    {
                        if (booking.dtToDate >= dtFromDate && booking.dtFromDate < dtFromDate)
                        {
                            numTotalRoomsBooked = numTotalRoomsBooked + (booking.dtToDate.Value - dtFromDate).TotalDays;
                        }
                        else if (booking.dtFromDate <= dtToDate && booking.dtToDate > dtToDate)
                        {
                            numTotalRoomsBooked = numTotalRoomsBooked + (dtToDate - booking.dtFromDate.Value).TotalDays;
                        }
                        else
                        {
                            numTotalRoomsBooked = numTotalRoomsBooked + (booking.dtToDate.Value.AddDays(1) - booking.dtFromDate).Value.TotalDays;
                        }
                    }

                    _occupancyByRooms.Add(new OccupancyByRooms
                    {
                        varRoomType = roomType.varRoomTypeName,
                        numTotalRoomsAvailable = numTotalRoomsAvailable * numTotalDays,
                        numTotalRoomsBooked = numTotalRoomsBooked,
                        numOccupancy = numTotalRoomsBooked / (numTotalRoomsAvailable * numTotalDays) * 100
                    });
                }

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/OccupancyByRoom.rdlc");
                ReportDataSource datasource = new ReportDataSource();
                datasource.Name = "OccupancyByRoomDataSet";
                datasource.Value = _occupancyByRooms;
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