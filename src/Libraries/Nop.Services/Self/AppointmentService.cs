using System;
using System.Collections.Generic;
using System.Text;
using Nop.Core.Data;
using Nop.Core.Domain.Self;
using Nop.Services.Events;
using System.Linq;

namespace Nop.Services.Self
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IRepository<Appointment> _appointmentRepository;

        public AppointmentService(
            IEventPublisher eventPublisher,
            IRepository<Appointment> appointmentRepository)
        {
            _eventPublisher = eventPublisher;
            _appointmentRepository = appointmentRepository;
        }

        public virtual Appointment GetAppointmentById(int appointmentId)
        {
            if (appointmentId == 0)
                return null;

            return _appointmentRepository.GetById(appointmentId);
        }

        /// <summary>
        /// Inserts a appointment
        /// </summary>
        /// <param name="appointment">Appointment</param>
        public virtual void InsertAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException(nameof(appointment));

            //insert
            _appointmentRepository.Insert(appointment);

            //event notification
            _eventPublisher.EntityInserted(appointment);
        }

        /// <summary>
        /// Updates the appointment
        /// </summary>
        /// <param name="appointment">Appointment</param>
        public virtual void UpdateAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException(nameof(appointment));

            //update
            _appointmentRepository.Update(appointment);

            //event notification
            _eventPublisher.EntityUpdated(appointment);
        }

        public virtual void DeleteAppointment(Appointment appointment)
        {
            if (appointment == null)
                throw new ArgumentNullException(nameof(appointment));

            //update
            _appointmentRepository.Delete(appointment);

            //event notification
            _eventPublisher.EntityDeleted(appointment);
        }

        public virtual List<Appointment> GetAppointmentsByResource(DateTime startTimeUtc, DateTime endTimeUtc, int resourceId)
        {
            var query = _appointmentRepository.Table
                .Where(x => x.ResourceId == resourceId)
                .Where(x => x.StartTimeUtc >= startTimeUtc && x.StartTimeUtc < endTimeUtc);

            return query.ToList();
        }

        public virtual List<Appointment> GetAppointmentsByVendor(int vendorId, DateTime startTimeUtc, DateTime endTimeUtc)
        {
            var query = _appointmentRepository.Table
                .Where(x => x.VendorId == vendorId)
                .Where(x => x.StartTimeUtc >= startTimeUtc && x.StartTimeUtc < endTimeUtc);

            return query.ToList();
        }
        
        public virtual List<Appointment> GetAvailableAppointmentsByCustomer(DateTime startTimeUtc, DateTime endTimeUtc, int resourceId, int customerId)
        {
            var query = _appointmentRepository.Table
                .Where(x => x.ResourceId == resourceId)
                .Where(x => x.CustomerId == 0 || x.CustomerId == customerId)
                .Where(x => x.StartTimeUtc >= startTimeUtc && x.StartTimeUtc < endTimeUtc);

            return query.ToList();
        }
    }
}
