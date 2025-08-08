using System;

namespace KSL_HMS.Models
{
    public class CalendarEvent
    {
        public int id { get; set; }
        public string resourceId { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string title { get; set; }
        public string backgroundColor { get; set; }
        public string textColor { get; set; }
        public string borderColor { get; set; }
    }
}