using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Data.Entity;
using System.Linq;

namespace KSL_HMS.Views.Reports
{
    public partial class TransportRequest : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class TransportRequests
        {
            public string varBookingRefferenceNo { get; set; }
            public string varRoomNo { get; set; }
            public string varGuestName { get; set; }
            public string dtCheckInDate { get; set; }
            public string varComment { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Convert.ToDateTime(Session["dtStart"].ToString());
                var EDate = Convert.ToDateTime(Session["dtEnd"].ToString());

                var TransportRequest = db.BookingHeaders.Where(bh => bh.bitActive == true && bh.bitTransportRequired == true && DbFunctions.TruncateTime(bh.dtToDate) >= DbFunctions.TruncateTime(SDate) && DbFunctions.TruncateTime(bh.dtFromDate) <= DbFunctions.TruncateTime(EDate)).Select(x => new TransportRequests
                {
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    varRoomNo = x.RoomType.varRoomTypeName + " - " + x.Room.varRoomNo,
                    varGuestName = db.BookingDetails.Where(b => b.BookingHeader.BookingRefference.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.bitPrimaryGuest == true).Select(b => b.Guest.varGuestName).FirstOrDefault(),
                    dtCheckInDate = x.dtFromDate.ToString(),
                    varComment = x.varFlightDetails
                }).ToList();

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/TransportRequest.rdlc");
                ReportDataSource datasource = new ReportDataSource();
                datasource.Name = "TransportRequestDataSet";
                datasource.Value = TransportRequest;
                ReportViewer1.LocalReport.DataSources.Add(datasource);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Transport Requests";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("RepoStDate", SDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", EDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}