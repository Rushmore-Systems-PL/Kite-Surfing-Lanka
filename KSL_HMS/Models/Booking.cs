using System;

namespace KSL_HMS.Models
{
    public class Booking
    {

        public int numIndex { get; set; }
        public int numBookingID { get; set; }
        public DateTime dtFromDate { get; set; }
        public DateTime dtToDate { get; set; }
        public string numRoomTypeID { get; set; }
        public int? numRoomID { get; set; }
        public bool bitActive { get; set; }
    }

    public class SelectedRoomBooking
    {
        public int numRoomtypeID { get; set; }
        public string varRoomtype { get; set; }
        public string varRoomDescription { get; set; }
        public int numRoomCount { get; set; }
        public decimal numRatePerPeriod { get; set; }
        public decimal numRatePerRoom { get; set; }
    }

    public class ExtBooking
    {
        public int numIndex { get; set; }
        public int numBookingID { get; set; }
        public string dtFromDate { get; set; }
        public string dtToDate { get; set; }
        public int numRoomTypeID { get; set; }
        public int numRoomID { get; set; }
        public int numPaxCount { get; set; }
        public bool bitLessonRequired { get; set; }
        public string dtLessonFrom { get; set; }
        public string dtLessonTo { get; set; }
        public bool bitRentalRequired { get; set; }
        public string dtRentalFrom { get; set; }
        public string dtRentalTo { get; set; }
        public bool bitActive { get; set; }
    }
}