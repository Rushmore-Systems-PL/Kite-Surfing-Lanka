using KSL_HMS.DB;
using KSL_HMS.Models;
using System.Linq;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class ReportsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                var customer = (from g in db.Guests
                                join bd in db.BookingDetails on g.numGuestID equals bd.numGuestID
                                join bh in db.BookingHeaders on bd.numBookingHeaderID equals bh.numBookingHeaderID
                                join bref in db.BookingRefferences on bh.numBookingRefferenceID equals bref.numBookingRefferenceID
                                where g.bitActive == true && bd.bitActive == true && bh.bitActive == true && bref.bitActive == true && bref.bitExternalBooking == true
                                select new
                                {
                                    Gid = g.numGuestID,
                                    Gdesc = "REF: " + bref.varBookingRefferenceNo + " | NAME: " + g.varGuestName
                                }).OrderBy(g => g.Gdesc).ToList();
                ViewBag.numGuestID = new SelectList((customer), "Gid", "Gdesc");
                var GuestIDs = (from g in db.Guests
                                join bd in db.BookingDetails on g.numGuestID equals bd.numGuestID
                                where g.bitActive == true && bd.bitActive == true
                                select g.numGuestID).Distinct().ToList();
                var Guests = (from g in db.Guests
                              where GuestIDs.Contains(g.numGuestID) && g.bitActive == true
                              select new
                              {
                                  Gid = g.numGuestID,
                                  Gdesc = "Name: " + g.varGuestName + " | Age: " + g.GuestChargeType.varGuestChargeTypeName + (g.varGuestPPNo != null ? " | PPNO/NIC: " + g.varGuestPPNo : "") + (g.varGuestEmail != null ? " | Email: " + g.varGuestEmail : "")
                              }).ToList();
                ViewBag.numInternalGuestID = new SelectList((Guests), "Gid", "Gdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        public ActionResult Generate(Report report)
        {
            this.ClearReportSessions();

            Session["dtStart"] = report.dtStart;
            Session["dtEnd"] = report.dtEnd;
            Session["numGuestID"] = report.numGuestID;
            Session["numInternalGuestID"] = report.numInternalGuestID;

            int LoadingReport = 0;

            if (report.numReportType == 1)
            {
                if (report.numRequestType == 1)
                {
                    LoadingReport = 1;
                }
                else if (report.numRequestType == 2)
                {
                    LoadingReport = 2;
                }
                else
                {
                    LoadingReport = 3;
                }
            }
            else if (report.numReportType == 2)
            {
                if (report.numOccupancyType == 1)
                {
                    LoadingReport = 4;
                }
                else
                {
                    LoadingReport = 5;
                }
            }
            else if (report.numReportType == 3)
            {
                LoadingReport = 6;
            }
            else if (report.numReportType == 4)
            {
                LoadingReport = 7;
            }
            else if (report.numReportType == 5)
            {
                LoadingReport = 8;
            }
            else
            {
                LoadingReport = 9;
            }

            return RedirectToAction("Viewer", new { id = LoadingReport });
        }

        public ActionResult Viewer(int id)
        {
            if (Session["UserID"] != null)
            {
                ViewBag.ReportID = id;
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public void ClearReportSessions()
        {
            Session.Remove("dtStart");
            Session.Remove("dtEnd");
            Session.Remove("numGuestID");
            Session.Remove("numInternalGuestID");
            return;
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