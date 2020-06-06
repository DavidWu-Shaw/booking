using Nop.Core.Domain.Self;
using Nop.Services.Helpers;
using System;

namespace Nop.Web.Models.Self
{
    public partial class AppointmentModelFactory : IAppointmentModelFactory
    {
        private readonly IDateTimeHelper _dateTimeHelper;

        public AppointmentModelFactory(IDateTimeHelper dateTimeHelper)
        {
            _dateTimeHelper = dateTimeHelper;
        }

        public virtual AppointmentUpdateModel PrepareAppointmentUpdateModel(Appointment appointment)
        {
            var model = new AppointmentUpdateModel();
            if (appointment != null)
            {
                model.Id = appointment.Id;
                var start = _dateTimeHelper.ConvertToUserTime(appointment.StartTimeUtc, DateTimeKind.Utc);
                var end = _dateTimeHelper.ConvertToUserTime(appointment.EndTimeUtc, DateTimeKind.Utc);
                model.TimeSlot = $"{start.ToShortTimeString()} - {end.ToShortTimeString()}, {start.ToShortDateString()} {start.ToString("dddd")}";
                model.Status = appointment.Status;
                model.Notes = appointment.Notes;
            }

            return model;
        }

        public virtual AppointmentInfoModel PrepareAppointmentInfoModel(Appointment appointment)
        {
            var model = new AppointmentInfoModel
            {
                id = appointment.Id.ToString(),
                start = _dateTimeHelper.ConvertToUserTime(appointment.StartTimeUtc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss"),
                end = _dateTimeHelper.ConvertToUserTime(appointment.EndTimeUtc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss"),
                resource = appointment.ResourceId.ToString()
            };
            model.tags = new TagModel
            {
                status = appointment.Status.ToString(),
                doctor = appointment.Product.Name
            };
            if (appointment.Customer != null)
            {
                model.text = appointment.Customer.Username;
            };

            return model;
        }

        public virtual VendorAppointmentInfoModel PrepareVendorAppointmentInfoModel(Appointment appointment)
        {
            var model = new VendorAppointmentInfoModel
            {
                id = appointment.Id.ToString(),
                start = _dateTimeHelper.ConvertToUserTime(appointment.StartTimeUtc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss"),
                end = _dateTimeHelper.ConvertToUserTime(appointment.EndTimeUtc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss"),
                resource = appointment.ResourceId.ToString()
            };

            return model;
        }            
    }
}
