using Nop.Core.Domain.Self;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Web.Models.Self
{
    public class AppointmentUpdateModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Product.Appointment.Fields.TimeSlot")]
        public string TimeSlot { get; set; }
        [NopResourceDisplayName("Product.Appointment.Fields.Status")]
        public AppointmentStatusType Status { get; set; }
        [NopResourceDisplayName("Product.Appointment.Fields.Notes")]
        public string Notes { get; set; }
    }
}
