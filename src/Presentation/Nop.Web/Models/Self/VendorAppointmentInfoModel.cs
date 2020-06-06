using System;

namespace Nop.Web.Models.Self
{
    public class VendorAppointmentInfoModel
    {
        public string id { get; set; }
        public string text { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string resource { get; set; }
        public string resourceName { get; set; }
        public string backColor { get; set; }
        public string bubbleHtml { get; set; }
        public string vendorId { get; set; }
        public bool moveDisabled { get; set; }
        public bool resizeDisabled { get; set; }
        public bool clickDisabled { get; set; }
    }
}
