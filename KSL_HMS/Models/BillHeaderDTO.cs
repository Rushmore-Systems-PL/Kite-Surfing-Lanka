using System;

namespace KSL_HMS.Models
{
    public class BillHeaderDTO
    {
        public Nullable<int> numBookingRefferenceID { get; set; }
        public string varBookingRefferenceNo { get; set; }
        public Nullable<decimal> numTotalCost { get; set; }
        public Nullable<decimal> numPayedAmount { get; set; }
        public Nullable<decimal> numBalanceToPay { get; set; }
        public Nullable<System.DateTime> dtCreatedDate { get; set; }
    }
}