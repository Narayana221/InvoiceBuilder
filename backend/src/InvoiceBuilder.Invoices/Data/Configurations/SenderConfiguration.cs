using InvoiceBuilder.Invoices.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Invoices.Data.Configurations;

public class SenderConfiguration : IEntityTypeConfiguration<Sender>
{
    public void Configure(EntityTypeBuilder<Sender> builder)
    {
        builder.ToTable("senders");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ContactName).HasMaxLength(200);
        builder.Property(s => s.AddressLine).IsRequired().HasMaxLength(300);
        builder.Property(s => s.City).IsRequired().HasMaxLength(100);
        builder.Property(s => s.PostalCode).HasMaxLength(20);
        builder.Property(s => s.Country).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.TaxId).HasMaxLength(50);
        builder.Property(s => s.BankDetails).HasMaxLength(200);

        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}
