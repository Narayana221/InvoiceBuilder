using InvoiceBuilder.Invoices.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Invoices.Data;

public class InvoicesDbContext(DbContextOptions<InvoicesDbContext> options) : DbContext(options)
{
    public DbSet<Sender> Senders => Set<Sender>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoicesDbContext).Assembly);
    }
}
