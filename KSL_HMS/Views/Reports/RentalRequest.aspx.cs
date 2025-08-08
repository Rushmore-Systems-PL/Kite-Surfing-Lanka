using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

namespace KSL_HMS.Views.Reports
{
    public partial class RentalRequest : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class RentalRequests
        {
            public string varBookingRefferenceNo { get; set; }
            public string varRoomNo { get; set; }
            public string varGuestName { get; set; }
            public string dtFromDate { get; set; }
            public string dtToDate { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Convert.ToDateTime(Session["dtStart"].ToString());
                var EDate = Convert.ToDateTime(Session["dtEnd"].ToString());

                var RentalRequest = db.BookingHeaders.Where(bh => bh.bitActive == true && bh.bitKSRentalRequired == true && DbFunctions.TruncateTime(bh.dtKSRentalRequiredTo) >= DbFunctions.TruncateTime(SDate) && DbFunctions.TruncateTime(bh.dtKSRentalRequiredFrom) <= DbFunctions.TruncateTime(EDate)).Select(x => new RentalRequests
                {
                    varBookingRefferenceNo = x.BookingRefference.varBookingRefferenceNo,
                    varRoomNo = x.RoomType.varRoomTypeName + " - " + x.Room.varRoomNo,
                    varGuestName = db.BookingDetails.Where(b => b.BookingHeader.BookingRefference.numBookingRefferenceID == x.numBookingRefferenceID && b.bitActive == true && b.bitPrimaryGuest == true).Select(b => b.Guest.varGuestName).FirstOrDefault(),
                    dtFromDate = x.dtKSRentalRequiredFrom.ToString(),
                    dtToDate = x.dtKSRentalRequiredTo.ToString(),
                }).ToList();

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/RentalRequest.rdlc");
                ReportDataSource datasource = new ReportDataSource();
                datasource.Name = "RentalRequestDataSet";
                datasource.Value = RentalRequest;
                ReportViewer1.LocalReport.DataSources.Add(datasource);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Rental Requests";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("RepoStDate", SDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", EDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}