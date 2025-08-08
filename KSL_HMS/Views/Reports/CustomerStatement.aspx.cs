using KSL_HMS.DB;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace KSL_HMS.Views.Reports
{
    public partial class CustomerStatement : System.Web.Mvc.ViewPage
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public class StatementSummary
        {
            public string varType { get; set; }
            public string varDescription { get; set; }
            public DateTime dtCreatedDate { get; set; }
            public decimal numAmount { get; set; }
        }

        public class CustomerStatements
        {
            public string dtCreatedDate { get; set; }
            public string varDescription { get; set; }
            public decimal numDebitAmount { get; set; }
            public decimal numCreditAmount { get; set; }
            public decimal numBalance { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                var SDate = Convert.ToDateTime(Session["dtStart"].ToString());
                var EDate = Convert.ToDateTime(Session["dtEnd"].ToString());
                var GuestID = Convert.ToInt32(Session["numGuestID"]);

                string varGuestName = db.Guests.Where(r => r.numGuestID == GuestID).Select(r => r.varGuestName).FirstOrDefault();
                decimal numBalance = 0;
                var numBilBalance = (from bid in db.BillDetails
                                     join bih in db.BillHeaders on bid.numBillHeaderID equals bih.numBillHeaderID
                                     join bh in db.BookingHeaders on bih.numBookingHeaderID equals bh.numBookingHeaderID
                                     join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                                     where bid.bitActive == true && bih.bitActive == true && bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                                     && DbFunctions.TruncateTime(bid.dtCreatedDate) < DbFunctions.TruncateTime(SDate)
                                     select bid.numFinalCost).Sum();

                var numPayBalance = (from re in db.Receipts
                                     join bref in db.BookingRefferences on re.numBookingRefferenceID equals bref.numBookingRefferenceID
                                     join bh in db.BookingHeaders on bref.numBookingRefferenceID equals bh.numBookingRefferenceID
                                     join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                                     where re.bitActive == true && bref.bitActive == true && bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                                     && DbFunctions.TruncateTime(re.dtCreatedDate) < DbFunctions.TruncateTime(SDate)
                                     select re.numAmount).Sum();

                numBalance = Convert.ToDecimal(numBilBalance) - Convert.ToDecimal(numPayBalance);

                var BIL = (from bid in db.BillDetails
                           join bih in db.BillHeaders on bid.numBillHeaderID equals bih.numBillHeaderID
                           join bh in db.BookingHeaders on bih.numBookingHeaderID equals bh.numBookingHeaderID
                           join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                           where bid.bitActive == true && bih.bitActive == true && bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                           && DbFunctions.TruncateTime(bid.dtCreatedDate) >= DbFunctions.TruncateTime(SDate)
                           && DbFunctions.TruncateTime(bid.dtCreatedDate) <= DbFunctions.TruncateTime(EDate)
                           select new StatementSummary
                           {
                               varType = "BILL",
                               varDescription = bid.ChargeType.varChargeTypeName == "Room" ? bid.ChargeType.varChargeTypeName + " | " + bid.Room.varRoomNo : bid.ChargeType.varChargeTypeName + " | " + bid.Item.varItemName,
                               dtCreatedDate = bid.dtCreatedDate.Value,
                               numAmount = bid.numFinalCost.Value
                           }).ToList();

                var PAY = (from re in db.Receipts
                           join bref in db.BookingRefferences on re.numBookingRefferenceID equals bref.numBookingRefferenceID
                           join bh in db.BookingHeaders on bref.numBookingRefferenceID equals bh.numBookingRefferenceID
                           join bd in db.BookingDetails on bh.numBookingHeaderID equals bd.numBookingHeaderID
                           where re.bitActive == true && bref.bitActive == true && bh.bitActive == true && bd.bitActive == true && bd.numGuestID == GuestID
                           && DbFunctions.TruncateTime(re.dtCreatedDate) >= DbFunctions.TruncateTime(SDate)
                           && DbFunctions.TruncateTime(re.dtCreatedDate) <= DbFunctions.TruncateTime(EDate)
                           select new StatementSummary
                           {
                               varType = "PAYMENT",
                               varDescription = re.ReciptType.varReciptTypeName + " | " + re.PaymentType.varPaymentTypeName,
                               dtCreatedDate = re.dtCreatedDate.Value,
                               numAmount = re.numAmount.Value
                           }).ToList();

                var Statement = BIL.Union(PAY).OrderBy(s => s.dtCreatedDate).ToList();

                List<CustomerStatements> CustomerStatement = new List<CustomerStatements>();
                CustomerStatement.Add(new CustomerStatements
                {
                    varDescription = "Balance B/F",
                    numBalance = numBalance
                });

                foreach (var item in Statement)
                {
                    if (item.varType == "BILL")
                    {
                        numBalance = numBalance + item.numAmount;
                        CustomerStatement.Add(new CustomerStatements
                        {
                            dtCreatedDate = item.dtCreatedDate.ToString("yyyy-MM-dd"),
                            varDescription = item.varDescription,
                            numDebitAmount = item.numAmount,
                            numBalance = numBalance
                        });
                    }
                    else
                    {
                        numBalance = numBalance - item.numAmount;
                        CustomerStatement.Add(new CustomerStatements
                        {
                            dtCreatedDate = item.dtCreatedDate.ToString("yyyy-MM-dd"),
                            varDescription = item.varDescription,
                            numCreditAmount = item.numAmount,
                            numBalance = numBalance
                        });
                    }
                }
                CustomerStatement.Add(new CustomerStatements { });
                CustomerStatement.Add(new CustomerStatements
                {
                    dtCreatedDate = EDate.ToString("yyyy-MM-dd"),
                    varDescription = "Balance",
                    numBalance = numBalance
                });

                ReportViewer1.ProcessingMode = ProcessingMode.Local;
                ReportViewer1.LocalReport.ReportPath = Server.MapPath("~/Reports/CustomerStatement.rdlc");
                ReportDataSource datasource = new ReportDataSource();
                datasource.Name = "CustomerStatementDataSet";
                datasource.Value = CustomerStatement;
                ReportViewer1.LocalReport.DataSources.Add(datasource);
                ReportViewer1.LocalReport.Refresh();
                ReportViewer1.LocalReport.DisplayName = "Customer Statement";
                ReportParameterCollection reportParameters = new ReportParameterCollection();
                reportParameters.Add(new ReportParameter("GuestName", varGuestName));
                reportParameters.Add(new ReportParameter("RepoStDate", SDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("RepoEnDate", EDate.ToString("yyyy-MM-dd")));
                reportParameters.Add(new ReportParameter("GeneratedDate", System.DateTime.Now.ToString("yyyy-MM-dd")));
                this.ReportViewer1.LocalReport.SetParameters(reportParameters);
            }
        }
    }
}