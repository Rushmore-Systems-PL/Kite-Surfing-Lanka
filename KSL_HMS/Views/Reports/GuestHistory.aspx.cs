using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Data.Entity;
using System.Linq;

namespace KSL_HMS.Views.Reports
{
    public partial class GuestHistory : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class GuestHistories
        {
            public DateTime dtFromDate { get; set; }
            public DateTime dtToDate { get; set; }
            public string varRefferenceNo { get; set; }
            public string varRoomType { get; set; }
            public string varRoomNo { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Convert.ToDateTime(Session["dtStart"].ToString());
                var EDate = Convert.ToDateTime(Session["dtEnd"].ToString());
                var GuestID = Convert.ToInt32(Session["numInternalGuestID"]);
                var GestName = db.Guests.Where(g => g.numGuestID == GuestID).Select(g => g.varGuestName).FirstOrDefault();

                var GuestHistories1 = (from bh in db.BookingHeaders
                                       join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                                       where bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                                       && DbFunctions.TruncateTime(bh.dtToDate) >= DbFunctions.TruncateTime(SDate)
                                       && DbFunctions.TruncateTime(bh.dtFromDate) <= DbFunctions.TruncateTime(EDate)
                                       select new GuestHistories
                                       {
                                           dtFromDate = bh.dtFromDate.Value,
                                           dtToDate = bh.dtToDate.Value,
                                           varRefferenceNo = bh.BookingRefference.varBookingRefferenceNo,
                                           varRoomNo = bh.Room.varRoomNo,
                                           varRoomType = bh.RoomType.varRoomTypeName
                                       }).ToList();

                var GuestHistories2 = (from bh in db.BookingHeaders
                                       join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                                       where bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                                       && DbFunctions.TruncateTime(bh.dtKsLessonRequiredTo) >= DbFunctions.TruncateTime(SDate)
                                       && DbFunctions.TruncateTime(bh.dtKsLessonRequiredFrom) <= DbFunctions.TruncateTime(EDate)
                                       select new GuestHistories
                                       {
                                           dtFromDate = bh.dtKsLessonRequiredFrom.Value,
                                           dtToDate = bh.dtKsLessonRequiredTo.Value,
                                           varRefferenceNo = bd.BookingHeader.BookingRefference.varBookingRefferenceNo,
                                           varRoomNo = bh.Room.varRoomNo,
                                           varRoomType = bh.RoomType.varRoomTypeName
                                       }).ToList();

                var GuestHistories3 = (from bh in db.BookingHeaders
                                       join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                                       where bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                                       && DbFunctions.TruncateTime(bh.dtKSRentalRequiredTo) >= DbFunctions.TruncateTime(SDate)
                                       && DbFunctions.TruncateTime(bh.dtKSRentalRequiredFrom) <= DbFunctions.TruncateTime(EDate)
                                       select new GuestHistories
                                       {
                                           dtFromDate = bh.dtKSRentalRequiredFrom.Value,
                                           dtToDate = bh.dtKSRentalRequiredTo.Value,
                                           varRefferenceNo = bd.BookingHeader.BookingRefference.varBookingRefferenceNo,
                                           varRoomNo = bh.Room.varRoomNo,
                                           varRoomType = bh.RoomType.varRoomTypeName
                                       }).ToList();

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/GuestHistory.rdlc");
                ReportDataSource datasource1 = new ReportDataSource("GuestHistoryDataSet1", GuestHistories1);
                ReportViewer1.LocalReport.DataSources.Add(datasource1);
                ReportDataSource datasource2 = new ReportDataSource("GuestHistoryDataSet2", GuestHistories2);
                ReportViewer1.LocalReport.DataSources.Add(datasource2);
                ReportDataSource datasource3 = new ReportDataSource("GuestHistoryDataSet3", GuestHistories3);
                ReportViewer1.LocalReport.DataSources.Add(datasource3);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Guest History";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("RepoStDate", SDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", EDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GestName", GestName));
                reportParameters.Add(new ReportParameter("GuestHistories1Count", GuestHistories1.Count().ToString()));
                reportParameters.Add(new ReportParameter("GuestHistories2Count", GuestHistories2.Count().ToString()));
                reportParameters.Add(new ReportParameter("GuestHistories3Count", GuestHistories3.Count().ToString()));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}