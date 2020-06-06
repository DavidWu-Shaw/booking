using Nop.Core.Domain.Self;

namespace Nop.Web.Areas.Admin.Models.Self
{
    public interface IAppointmentModelFactory
    {
        AppointmentEditModel PrepareAppointmentEditModel(Appointment appointment);
        AppointmentInfoModel PrepareAppointmentInfoModel(Appointment appointment);
    };
}
