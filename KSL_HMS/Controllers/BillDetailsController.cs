using KSL_HMS.DB;
using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace KSL_HMS.Controllers
{
    public class BillDetailsController : Controller
    {
        private ConnectionHMSDB db = new ConnectionHMSDB();

        public ActionResult AddToBill()
        {
            if (Session["UserID"] != null)
            {
                ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true && i.numLocationID != 3 && i.numLocationID != 4 && i.numLocationID != 5).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName");
                var Rooms = (from bh in db.BookingHeaders
                             join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                             where bh.bitCheckedIn == true && bh.bitActive == true && bf.bitClosed == false
                             select new
                             {
                                 Rid = bh.numBookingHeaderID,
                                 Rdesc = bf.bitExternalBooking == false ? "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo : "REF : " + bf.varBookingRefferenceNo,
                                 Rorder = bh.dtCreatedDate
                             }).OrderByDescending(x => x.Rorder).ToList();
                ViewBag.numRoomID = new SelectList(Rooms, "Rid", "Rdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult Loaditems(int id)
        {
            var items = (from i in db.Items
                         join it in db.ItemTypes on i.numItemTypeID equals it.numItemTypeID
                         join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                         where lit.bitActive == true && lit.numLocationID == id && i.bitIsDeleted == false && i.bitActive == true && i.bitIsDeleted == false && it.bitActive == true
                         select new
                         {
                             id = i.numItemID,
                             name = i.varItemName,
                             rate = i.numRatePerUnit,
                             order = i.varItemName
                         }).OrderBy(x => x.order).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoaditemRate(int id)
        {
            return Json(new { rate = db.Items.Where(l => l.bitActive == true && l.bitIsDeleted == false && l.numItemID == id).Select(x => x.numRatePerUnit) }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadGuests(int id)
        {
            return Json(db.BookingDetails.Where(b => b.bitActive == true && b.numBookingHeaderID == id).OrderBy(x => x.Guest.varGuestName).Select(x => new { id = x.numBookingDetailID, name = x.Guest.varGuestName }), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToBill(BillDetail billDetail)
        {
            BookingHeader bookingHeader = db.BookingHeaders.Find(billDetail.numRoomID);// To load Guest details have passed numBookingHeaderID
            var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == bookingHeader.numBookingHeaderID).Select(b => b.numBillHeaderID).FirstOrDefault();

            billDetail.numRoomID = bookingHeader.numRoomID;
            billDetail.numBillHeaderID = numBillHeaderID;
            billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Item").Select(c => c.numChargeTypeID).FirstOrDefault();
            if (billDetail.numDiscount != null)
            {
                billDetail.bitDiscountOffered = true;
            }
            else
            {
                billDetail.bitDiscountOffered = false;
            }
            billDetail.bitActive = true;
            billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            billDetail.dtCreatedDate = System.DateTime.Now;
            db.BillDetails.Add(billDetail);
            db.SaveChanges();

            BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
            billHeader.numTotalCost = billHeader.numTotalCost + billDetail.numFinalCost;
            //if (billHeader.numPayedAmount == 0)
            //{
            //    billHeader.numBalanceToPay = billHeader.numBalanceToPay + billDetail.numFinalCost;
            //}
            //else
            //{
            //    billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
            //}
            billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            billHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            //var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
            //if (numBalanceToPayRefference == 0)
            //{
            //    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
            //    bookingRefference.bitPayed = true;
            //    db.Entry(bookingRefference).State = EntityState.Modified;
            //    db.SaveChanges();
            //}
            //else
            //{
            //    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
            //    bookingRefference.bitPayed = false;
            //    db.Entry(bookingRefference).State = EntityState.Modified;
            //    db.SaveChanges();
            //}

            TempData["BillDetailStatus"] = "Saved";
            return RedirectToAction("AddToBill");
        }

        public ActionResult Index()
        {
            if (Session["UserID"] != null)
            {
                var billDetails = db.BillDetails.Where(b => b.BillHeader.BookingHeader.BookingRefference.bitClosed == false && b.BillHeader.BookingHeader.BookingRefference.bitActive == true && b.BillHeader.BookingHeader.bitActive == true).Include(b => b.ChargeType).Include(b => b.Item).Include(b => b.Room).Include(b => b.BillHeader).Include(b => b.BookingDetail).Where(b => b.bitActive == true);
                return View(billDetails.OrderByDescending(x => x.dtCreatedDate).ToList());
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult EditAddToBill(int? id)
        {
            if (Session["UserID"] != null)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                BillDetail billDetail = db.BillDetails.Find(id);
                if (billDetail == null)
                {
                    return HttpNotFound();
                }

                ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName", billDetail.numLocationID);
                var Rooms = (from bh in db.BookingHeaders
                             join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                             where bh.bitCheckedIn == true && bh.bitCheckedOut == false && bh.bitActive == true && bf.bitClosed == false
                             select new
                             {
                                 Rid = bh.numBookingHeaderID,
                                 Rdesc = bf.bitExternalBooking == false ? "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo : "REF : " + bf.varBookingRefferenceNo,
                                 Rorder = bh.dtCreatedDate
                             }).OrderByDescending(x => x.Rorder).ToList();
                ViewBag.numRoomID = new SelectList(Rooms, "Rid", "Rdesc", billDetail.BillHeader.numBookingHeaderID);
                ViewBag.numBillHeaderID = new SelectList(db.BillHeaders, "numBillHeaderID", "numBillHeaderID", billDetail.numBillHeaderID);
                ViewBag.numChargeTypeID = new SelectList(db.ChargeTypes, "numChargeTypeID", "varChargeTypeName", billDetail.numChargeTypeID);
                return View(billDetail);
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        public ActionResult LoadDefItem(int id)
        {
            return Json(db.Items.Where(p => p.numItemID == id).Select(s => new { id = s.numItemID, name = s.varItemName }).FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadNotInDefItem(int Iid, int Lid)
        {
            var items = (from i in db.Items
                         join it in db.ItemTypes on i.numItemTypeID equals it.numItemTypeID
                         join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                         where lit.bitActive == true && lit.numLocationID == Lid && i.numItemID != Iid && i.bitIsDeleted == false && i.bitActive == true && it.bitActive == true
                         select new
                         {
                             id = i.numItemID,
                             name = i.varItemName,
                             rate = i.numRatePerUnit,
                             order = i.varItemName
                         }).OrderBy(x => x.order).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadDefGuest(int id)
        {
            return Json(db.BookingDetails.Where(b => b.numBookingDetailID == id).Select(x => new { id = x.numBookingDetailID, name = x.Guest.varGuestName }).FirstOrDefault(), JsonRequestBehavior.AllowGet);
        }

        public ActionResult LoadNotInDefGuest(int Hid, int Did)
        {
            return Json(db.BookingDetails.Where(b => b.bitActive == true && b.numBookingHeaderID == Hid && b.numBookingDetailID != Did).OrderBy(x => x.Guest.varGuestName).Select(x => new { id = x.numBookingDetailID, name = x.Guest.varGuestName }), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAddToBill(BillDetail billDetail)
        {
            BookingHeader bookingHeader = db.BookingHeaders.Find(billDetail.numRoomID);// To load Guest details have passed numBookingHeaderID
            decimal OldBillAmount = db.BillDetails.Where(b => b.numBillDetailID == billDetail.numBillDetailID).Select(b => b.numFinalCost.Value).FirstOrDefault();

            billDetail.numRoomID = bookingHeader.numRoomID;
            if (billDetail.numDiscount != null)
            {
                billDetail.bitDiscountOffered = true;
            }
            else
            {
                billDetail.bitDiscountOffered = false;
            }
            billDetail.bitActive = true;
            billDetail.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
            billDetail.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billDetail).State = EntityState.Modified;
            db.SaveChanges();

            if (billDetail.numFinalCost != OldBillAmount)
            {
                BillHeader billHeader = db.BillHeaders.Find(billDetail.numBillHeaderID);
                billHeader.numTotalCost = billHeader.numTotalCost - OldBillAmount;
                billHeader.numBalanceToPay = billHeader.numBalanceToPay - OldBillAmount;

                billHeader.numTotalCost = billHeader.numTotalCost + billDetail.numFinalCost;
                if (billHeader.numPayedAmount == 0)
                {
                    billHeader.numBalanceToPay = billHeader.numBalanceToPay + billDetail.numFinalCost;
                }
                else
                {
                    billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
                }
                billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]);
                billHeader.dtUpdatetDate = System.DateTime.Now;
                db.Entry(billHeader).State = EntityState.Modified;
                db.SaveChanges();

                var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
                if (numBalanceToPayRefference == 0)
                {
                    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                    bookingRefference.bitPayed = true;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                    bookingRefference.bitPayed = false;
                    db.Entry(bookingRefference).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            TempData["BillDetailStatus"] = "Edited";
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BillDetail billDetail = db.BillDetails.Find(id);
            if (billDetail == null)
            {
                return HttpNotFound();
            }

            billDetail.bitActive = false;
            billDetail.numDeletedByID = Convert.ToInt32(Session["UserID"]); ;
            billDetail.dtDeletedDate = System.DateTime.Now;

            db.Entry(billDetail).State = EntityState.Modified;
            db.SaveChanges();

            BillHeader billHeader = db.BillHeaders.Find(billDetail.numBillHeaderID);
            billHeader.numTotalCost = billHeader.numTotalCost - billDetail.numFinalCost;
            billHeader.numBalanceToPay = billHeader.numBalanceToPay - billDetail.numFinalCost;
            billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            billHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            TempData["BillDetailStatus"] = "Deleted";
            return RedirectToAction("Index");
        }

        public ActionResult AddToBar()
        {
            if (Session["UserID"] != null)
            {
                var numBarLocatioID = Convert.ToInt32(Session["BarLocationID"]);
                ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true && i.numLocationID == numBarLocatioID).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName", numBarLocatioID);

                var items = (from i in db.Items
                             join it in db.ItemTypes on i.numItemTypeID equals it.numItemTypeID
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where lit.bitActive == true && lit.numLocationID == numBarLocatioID && i.bitIsDeleted == false && i.bitActive == true && it.bitActive == true
                             select new
                             {
                                 id = i.numItemID,
                                 name = i.varItemName,
                                 rate = i.numRatePerUnit,
                                 order = i.varItemName
                             }).OrderBy(x => x.order).ToList();
                ViewBag.numItemID = new SelectList(items, "id", "name");

                var Rooms = (from bh in db.BookingHeaders
                             join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                             where bh.bitCheckedIn == true && bh.bitCheckedOut == false && bh.bitActive == true && bf.bitClosed == false
                             select new
                             {
                                 Rid = bh.numBookingHeaderID,
                                 Rdesc = bf.bitExternalBooking == false ? "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo : "REF : " + bf.varBookingRefferenceNo,
                                 Rorder = bh.dtCreatedDate
                             }).OrderByDescending(x => x.Rorder).ToList();
                ViewBag.numRoomID = new SelectList(Rooms, "Rid", "Rdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToBar(BillDetail billDetail)
        {
            BookingHeader bookingHeader = db.BookingHeaders.Find(billDetail.numRoomID);// To load Guest details have passed numBookingHeaderID
            var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == bookingHeader.numBookingHeaderID).Select(b => b.numBillHeaderID).FirstOrDefault();

            billDetail.numRoomID = bookingHeader.numRoomID;
            billDetail.numBillHeaderID = numBillHeaderID;
            billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Item").Select(c => c.numChargeTypeID).FirstOrDefault();
            if (billDetail.numDiscount != null)
            {
                billDetail.bitDiscountOffered = true;
            }
            else
            {
                billDetail.bitDiscountOffered = false;
            }
            billDetail.bitActive = true;
            billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            billDetail.dtCreatedDate = System.DateTime.Now;
            db.BillDetails.Add(billDetail);
            db.SaveChanges();

            BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
            billHeader.numTotalCost = billHeader.numTotalCost + billDetail.numFinalCost;
            if (billHeader.numPayedAmount == 0)
            {
                billHeader.numBalanceToPay = billHeader.numBalanceToPay + billDetail.numFinalCost;
            }
            else
            {
                billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
            }
            billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            billHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
            if (numBalanceToPayRefference == 0)
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = true;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = false;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }

            TempData["BillDetailStatus"] = "Saved";
            return RedirectToAction("AddToBar");
        }

        public ActionResult AddToRepairShop()
        {
            if (Session["UserID"] != null)
            {
                var numRepairShopLocationID = Convert.ToInt32(Session["RepairShopLocationID"]);
                ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true && i.numLocationID == numRepairShopLocationID).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName", numRepairShopLocationID);

                var items = (from i in db.Items
                             join it in db.ItemTypes on i.numItemTypeID equals it.numItemTypeID
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where lit.bitActive == true && lit.numLocationID == numRepairShopLocationID && i.bitIsDeleted == false && i.bitActive == true && it.bitActive == true
                             select new
                             {
                                 id = i.numItemID,
                                 name = i.varItemName,
                                 rate = i.numRatePerUnit,
                                 order = i.varItemName
                             }).OrderBy(x => x.order).ToList();
                ViewBag.numItemID = new SelectList(items, "id", "name");

                var Rooms = (from bh in db.BookingHeaders
                             join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                             where bh.bitCheckedIn == true && bh.bitCheckedOut == false && bh.bitActive == true && bf.bitClosed == false
                             select new
                             {
                                 Rid = bh.numBookingHeaderID,
                                 Rdesc = bf.bitExternalBooking == false ? "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo : "REF : " + bf.varBookingRefferenceNo,
                                 Rorder = bh.dtCreatedDate
                             }).OrderByDescending(x => x.Rorder).ToList();
                ViewBag.numRoomID = new SelectList(Rooms, "Rid", "Rdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToRepairShop(BillDetail billDetail)
        {
            BookingHeader bookingHeader = db.BookingHeaders.Find(billDetail.numRoomID);// To load Guest details have passed numBookingHeaderID
            var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == bookingHeader.numBookingHeaderID).Select(b => b.numBillHeaderID).FirstOrDefault();

            billDetail.numRoomID = bookingHeader.numRoomID;
            billDetail.numBillHeaderID = numBillHeaderID;
            billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Item").Select(c => c.numChargeTypeID).FirstOrDefault();
            if (billDetail.numDiscount != null)
            {
                billDetail.bitDiscountOffered = true;
            }
            else
            {
                billDetail.bitDiscountOffered = false;
            }
            billDetail.bitActive = true;
            billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            billDetail.dtCreatedDate = System.DateTime.Now;
            db.BillDetails.Add(billDetail);
            db.SaveChanges();

            BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
            billHeader.numTotalCost = billHeader.numTotalCost + billDetail.numFinalCost;
            if (billHeader.numPayedAmount == 0)
            {
                billHeader.numBalanceToPay = billHeader.numBalanceToPay + billDetail.numFinalCost;
            }
            else
            {
                billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
            }
            billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            billHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
            if (numBalanceToPayRefference == 0)
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = true;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = false;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }

            TempData["BillDetailStatus"] = "Saved";
            return RedirectToAction("AddToRepairShop");
        }

        public ActionResult AddToShop()
        {
            if (Session["UserID"] != null)
            {
                var numShopLocationID = Convert.ToInt32(Session["ShopLocationID"]);
                ViewBag.numLocationID = new SelectList(db.Locations.Where(i => i.bitActive == true && i.numLocationID == numShopLocationID).OrderBy(x => x.varLocationName), "numLocationID", "varLocationName", numShopLocationID);

                var items = (from i in db.Items
                             join it in db.ItemTypes on i.numItemTypeID equals it.numItemTypeID
                             join lit in db.LocationItemTypes on it.numItemTypeID equals lit.numItemTypeID
                             where lit.bitActive == true && lit.numLocationID == numShopLocationID && i.bitIsDeleted == false && i.bitActive == true && it.bitActive == true
                             select new
                             {
                                 id = i.numItemID,
                                 name = i.varItemName,
                                 rate = i.numRatePerUnit,
                                 order = i.varItemName
                             }).OrderBy(x => x.order).ToList();
                ViewBag.numItemID = new SelectList(items, "id", "name");

                var Rooms = (from bh in db.BookingHeaders
                             join bf in db.BookingRefferences on bh.numBookingRefferenceID equals bf.numBookingRefferenceID
                             where bh.bitCheckedIn == true && bh.bitCheckedOut == false && bh.bitActive == true && bf.bitClosed == false
                             select new
                             {
                                 Rid = bh.numBookingHeaderID,
                                 Rdesc = bf.bitExternalBooking == false ? "REF : " + bf.varBookingRefferenceNo + " | ROOM NO: " + bh.Room.varRoomNo : "REF : " + bf.varBookingRefferenceNo,
                                 Rorder = bh.dtCreatedDate
                             }).OrderByDescending(x => x.Rorder).ToList();
                ViewBag.numRoomID = new SelectList(Rooms, "Rid", "Rdesc");
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Users");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddToShop(BillDetail billDetail)
        {
            BookingHeader bookingHeader = db.BookingHeaders.Find(billDetail.numRoomID);// To load Guest details have passed numBookingHeaderID
            var numBillHeaderID = db.BillHeaders.Where(b => b.numBookingHeaderID == bookingHeader.numBookingHeaderID).Select(b => b.numBillHeaderID).FirstOrDefault();

            billDetail.numRoomID = bookingHeader.numRoomID;
            billDetail.numBillHeaderID = numBillHeaderID;
            billDetail.numChargeTypeID = db.ChargeTypes.Where(c => c.bitActive == true && c.varChargeTypeName == "Item").Select(c => c.numChargeTypeID).FirstOrDefault();
            if (billDetail.numDiscount != null)
            {
                billDetail.bitDiscountOffered = true;
            }
            else
            {
                billDetail.bitDiscountOffered = false;
            }
            billDetail.bitActive = true;
            billDetail.numCreatedByID = Convert.ToInt32(Session["UserID"]);
            billDetail.dtCreatedDate = System.DateTime.Now;
            db.BillDetails.Add(billDetail);
            db.SaveChanges();

            BillHeader billHeader = db.BillHeaders.Find(numBillHeaderID);
            billHeader.numTotalCost = billHeader.numTotalCost + billDetail.numFinalCost;
            if (billHeader.numPayedAmount == 0)
            {
                billHeader.numBalanceToPay = billHeader.numBalanceToPay + billDetail.numFinalCost;
            }
            else
            {
                billHeader.numBalanceToPay = billHeader.numTotalCost - billHeader.numPayedAmount;
            }
            billHeader.numUpdatetdByID = Convert.ToInt32(Session["UserID"]); ;
            billHeader.dtUpdatetDate = System.DateTime.Now;
            db.Entry(billHeader).State = EntityState.Modified;
            db.SaveChanges();

            var numBalanceToPayRefference = db.BillHeaders.Where(b => b.numBookingRefferenceID == billHeader.numBookingRefferenceID && b.bitActive == true && b.BookingHeader.bitActive == true && b.BookingRefference.bitActive == true).Select(b => b.numBalanceToPay.Value).Sum();
            if (numBalanceToPayRefference == 0)
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = true;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                BookingRefference bookingRefference = db.BookingRefferences.Find(billHeader.numBookingRefferenceID);
                bookingRefference.bitPayed = false;
                db.Entry(bookingRefference).State = EntityState.Modified;
                db.SaveChanges();
            }

            TempData["BillDetailStatus"] = "Saved";
            return RedirectToAction("AddToShop");
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