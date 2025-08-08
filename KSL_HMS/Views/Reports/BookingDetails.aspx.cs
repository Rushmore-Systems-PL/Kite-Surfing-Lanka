using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

namespace KSL_HMS.Views.Reports
{
    public partial class BookingDetails : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class BookingDetail
        {
            public string varRefferenceNo { get; set; }
            public string varRoomType { get; set; }
            public string varRoomNo { get; set; }
            public string MainGuest { get; set; }
            public string varGuestCount { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Convert.ToDateTime(Session["dtStart"].ToString());
                var EDate = Convert.ToDateTime(Session["dtEnd"].ToString());

                var BookingDetail1 = (from bh in db.BookingHeaders
                                      where bh.bitActive == true && bh.bitCheckedIn != true && DbFunctions.TruncateTime(bh.dtFromDate) >= DbFunctions.TruncateTime(SDate)
                                      && DbFunctions.TruncateTime(bh.dtFromDate) <= DbFunctions.TruncateTime(EDate)
                                      select new BookingDetail
                                      {
                                          varRefferenceNo = bh.BookingRefference.varBookingRefferenceNo,
                                          varRoomNo = bh.Room.varRoomNo,
                                          varRoomType = bh.RoomType.varRoomTypeName,
                                          MainGuest = db.BookingDetails.Where(d => d.numBookingHeaderID == bh.numBookingHeaderID && d.bitPrimaryGuest == true && d.bitActive == true).Select(d => d.Guest.varGuestName).FirstOrDefault(),
                                          varGuestCount = (bh.numAdultsCount + bh.numChildrensCount + bh.numInfantsCount).ToString(),
                                      }).ToList();

                var BookingDetail2 = (from bh in db.BookingHeaders
                                      where bh.bitActive == true && bh.bitCheckedIn == true && bh.bitCheckedOut != true && DbFunctions.TruncateTime(bh.dtToDate) >= DbFunctions.TruncateTime(SDate)
                                      && DbFunctions.TruncateTime(bh.dtToDate) <= DbFunctions.TruncateTime(EDate)
                                      select new BookingDetail
                                      {
                                          varRefferenceNo = bh.BookingRefference.varBookingRefferenceNo,
                                          varRoomNo = bh.Room.varRoomNo,
                                          varRoomType = bh.RoomType.varRoomTypeName,
                                          MainGuest = db.BookingDetails.Where(d => d.numBookingHeaderID == bh.numBookingHeaderID && d.bitPrimaryGuest == true && d.bitActive == true).Select(d => d.Guest.varGuestName).FirstOrDefault(),
                                          varGuestCount = (bh.numAdultsCount + bh.numChildrensCount + bh.numInfantsCount).ToString(),
                                      }).ToList();

                var BookingDetail3 = (from bh in db.BookingHeaders
                                      where bh.bitActive == true && bh.bitCheckedIn == true && bh.bitCheckedOut == false && DbFunctions.TruncateTime(bh.dtToDate) >= DbFunctions.TruncateTime(SDate)
                                      && DbFunctions.TruncateTime(bh.dtFromDate) <= DbFunctions.TruncateTime(EDate)
                                      select new BookingDetail
                                      {
                                          varRefferenceNo = bh.BookingRefference.varBookingRefferenceNo,
                                          varRoomNo = bh.Room.varRoomNo,
                                          varRoomType = bh.RoomType.varRoomTypeName,
                                          MainGuest = db.BookingDetails.Where(d => d.numBookingHeaderID == bh.numBookingHeaderID && d.bitPrimaryGuest == true && d.bitActive == true).Select(d => d.Guest.varGuestName).FirstOrDefault(),
                                          varGuestCount = (bh.numAdultsCount + bh.numChildrensCount + bh.numInfantsCount).ToString(),
                                      }).ToList();

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/BookingDetails.rdlc");
                ReportDataSource datasource1 = new ReportDataSource("BookingDetailsDataSet1", BookingDetail1);
                ReportViewer1.LocalReport.DataSources.Add(datasource1);
                ReportDataSource datasource2 = new ReportDataSource("BookingDetailsDataSet2", BookingDetail2);
                ReportViewer1.LocalReport.DataSources.Add(datasource2);
                ReportDataSource datasource3 = new ReportDataSource("BookingDetailsDataSet3", BookingDetail3);
                ReportViewer1.LocalReport.DataSources.Add(datasource3);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Daily Booking Details";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("RepoStDate", SDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", EDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("BookingDetail1Count", BookingDetail1.Count().ToString()));
                reportParameters.Add(new ReportParameter("BookingDetail2Count", BookingDetail2.Count().ToString()));
                reportParameters.Add(new ReportParameter("BookingDetail3Count", BookingDetail3.Count().ToString()));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}