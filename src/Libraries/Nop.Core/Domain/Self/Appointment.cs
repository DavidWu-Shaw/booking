using System;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;

namespace Nop.Core.Domain.Self
{
    public class Appointment : BaseEntity
    {
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTimeUtc { get; set; }
        public string Notes { get; set; }
        public short StatusId { get; set; }
        public int ResourceId { get; set; }
        public int CustomerId { get; set; }
        public int VendorId { get; set; }
        public virtual Product Product { get; set; }
        public virtual Customer Customer { get; set; }

        public AppointmentStatusType Status
        {
            get => (AppointmentStatusType)StatusId;
            set => StatusId = (short)value;
        }
    }
}
