using Nop.Core.Domain.Self;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;

namespace Nop.Web.Areas.Admin.Models.Self
{
    public class AppointmentEditModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.Appointment.Fields.TimeSlot")]
        public string TimeSlot { get; set; }
        [NopResourceDisplayName("Admin.Appointment.Fields.Status")]
        public AppointmentStatusType Status { get; set; }
        [NopResourceDisplayName("Admin.Appointment.Fields.Notes")]
        public string Notes { get; set; }
        public int ResourceId { get; set; }
        [NopResourceDisplayName("Admin.Appointment.Fields.Customer")]
        public int CustomerId { get; set; }

        [NopResourceDisplayName("Admin.Appointment.Fields.CustomerEmail")]
        public string CustomerEmail { get; set; }
        [NopResourceDisplayName("Admin.Appointment.Fields.CustomerFullName")]
        public string CustomerFullName { get; set; }
        public bool IsLoggedInAsVendor { get; set; }
    }
}
