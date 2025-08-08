using System;

namespace KSL_HMS.Models
{
    public class BookingHeaderDTO
    {
        public int numBookingRefferenceID { get; set; }
        public int numBookingHeaderID { get; set; }
        public string varBookingRefferenceNo { get; set; }
        public Nullable<bool> bitExternalBooking { get; set; }
        public string varRoomTypeName { get; set; }
        public string varRoomNo { get; set; }
        public Nullable<System.DateTime> dtFromDate { get; set; }
        public Nullable<System.DateTime> dtToDate { get; set; }
        public Nullable<int> numGuestCount { get; set; }
        public Nullable<bool> bitClosed { get; set; }
        public Nullable<bool> bitCheckedOut { get; set; }
        public Nullable<bool> bitCheckedIn { get; set; }
        public Nullable<System.DateTime> dtCreatedDate { get; set; }

    }
}