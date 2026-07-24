using InvoiceBuilder.Invoices.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Invoices.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ContactName).HasMaxLength(200);
        builder.Property(c => c.AddressLine).IsRequired().HasMaxLength(300);
        builder.Property(c => c.City).IsRequired().HasMaxLength(100);
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.Country).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.TaxId).HasMaxLength(50);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
