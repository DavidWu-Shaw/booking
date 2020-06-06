using System;

namespace Nop.Web.Models.Self
{
    public class AppointmentInfoModel
    {
        public string id { get; set; }
        public string text { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        public string resource { get; set; }
        public TagModel tags { get; set; }
    }

    public class TagModel
    {
        public string status { get; set; }
        public string doctor { get; set; }
    }
}
