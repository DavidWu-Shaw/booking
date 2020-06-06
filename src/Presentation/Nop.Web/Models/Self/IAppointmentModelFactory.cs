using Nop.Core.Domain.Self;

namespace Nop.Web.Models.Self
{
    public interface IAppointmentModelFactory
    {
        AppointmentUpdateModel PrepareAppointmentUpdateModel(Appointment appointment);
        AppointmentInfoModel PrepareAppointmentInfoModel(Appointment appointment);
        VendorAppointmentInfoModel PrepareVendorAppointmentInfoModel(Appointment appointment);
    };
}