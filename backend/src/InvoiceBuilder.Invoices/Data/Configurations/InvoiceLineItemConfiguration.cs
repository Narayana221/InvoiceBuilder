using InvoiceBuilder.Invoices.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvoiceBuilder.Invoices.Data.Configurations;

public class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("invoice_line_items");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.Description).IsRequired().HasMaxLength(500);
        builder.Property(li => li.Quantity).HasPrecision(18, 2);
        builder.Property(li => li.UnitPrice).HasPrecision(18, 2);

        builder.Ignore(li => li.LineTotal);

        builder.HasQueryFilter(li => !li.Invoice.IsDeleted);
    }
}
