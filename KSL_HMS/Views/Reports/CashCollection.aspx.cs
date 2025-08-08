using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Data.Entity;
using System.Linq;

namespace KSL_HMS.Views.Reports
{
    public partial class CashCollection : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class CashCollections
        {
            public string varPaymentCurrency { get; set; }
            public decimal numAmount { get; set; }
            public DateTime dtPaymentDate { get; set; }
            public string varBookingRefrerence { get; set; }
            public string varPaymentCategory { get; set; }
            public string varReciptCategory { get; set; }
            public decimal numEuroAmount { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Convert.ToDateTime(Session["dtStart"].ToString());
                var EDate = Convert.ToDateTime(Session["dtEnd"].ToString());

                var CashCollection = db.Receipts.Where(r => r.bitActive == true && DbFunctions.TruncateTime(r.dtCreatedDate) >= DbFunctions.TruncateTime(SDate) && DbFunctions.TruncateTime(r.dtCreatedDate) <= DbFunctions.TruncateTime(EDate)).Select(x => new CashCollections
                {
                    varPaymentCurrency = x.Currency.varCurrencyName,
                    numAmount = x.numPayInCurrencyAmount.Value,
                    dtPaymentDate = x.dtCreatedDate.Value,
                    varBookingRefrerence = x.BookingRefference.varBookingRefferenceNo,
                    varPaymentCategory = x.PaymentType.varPaymentTypeName,
                    varReciptCategory = x.ReciptType.varReciptTypeName,
                    numEuroAmount = x.numAmount.Value
                }).ToList();

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/CashCollection.rdlc");
                ReportDataSource datasource = new ReportDataSource();
                datasource.Name = "CashCollectionDataSet";
                datasource.Value = CashCollection;
                ReportViewer1.LocalReport.DataSources.Add(datasource);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Cash Collections";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("RepoStDate", SDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", EDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}