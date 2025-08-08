using System.ComponentModel.DataAnnotations;

namespace KSL_HMS.Models
{
    public class Report
    {
        [Required(ErrorMessage = "Please Select Date Range.")]
        public string varDateRange { get; set; }
        public string dtStart { get; set; }
        public string dtEnd { get; set; }
        public int numReportType { get; set; }
        public int numRequestType { get; set; }
        public int numOccupancyType { get; set; }
        public int numGuestID { get; set; }
        public int numInternalGuestID { get; set; }
    }
}