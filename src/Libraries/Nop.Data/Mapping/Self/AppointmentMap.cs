using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Self;

namespace Nop.Data.Mapping.Self
{
    public partial class AppointmentMap : NopEntityTypeConfiguration<Appointment>
    {
        #region Methods

        /// <summary>
        /// Configures the entity
        /// </summary>
        /// <param name="builder">The builder to be used to configure the entity</param>
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("tblAppointment");
            builder.HasKey(productAppointment => productAppointment.Id);

            builder.HasOne(productAppointment => productAppointment.Product)
                .WithMany()
                .HasForeignKey(productAppointment => productAppointment.ResourceId)
                .IsRequired();

            builder.HasOne(productAppointment => productAppointment.Customer)
                .WithMany()
                .HasForeignKey(productAppointment => productAppointment.CustomerId)
                .IsRequired();

            builder.Ignore(a => a.Status);

            base.Configure(builder);
        }

        #endregion
    }
}
