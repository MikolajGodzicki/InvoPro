using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using InvoPro.Models;
using System.IO;

namespace InvoPro.Data
{
    public class InvoiceDbContext : DbContext
    {
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<CompanyInfo> CompanyInfo { get; set; }
        public DbSet<Contractor> Contractors { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InvoPro", "invoices.db");
            
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
            optionsBuilder
                .UseSqlite($"Data Source={dbPath}")
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Number).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ClientAddress).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ClientNip).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.IssueDate).IsRequired();
                entity.Property(e => e.DueDate).IsRequired();
                
                entity.Ignore(e => e.TotalNet);
                entity.Ignore(e => e.TotalVat);
                entity.Ignore(e => e.TotalAmount);
                entity.Ignore(e => e.TotalNetFormatted);
                entity.Ignore(e => e.TotalVatFormatted);
                entity.Ignore(e => e.TotalAmountFormatted);
            });

            modelBuilder.Entity<InvoiceItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Quantity).IsRequired().HasColumnType("decimal(18,4)");
                entity.Property(e => e.UnitPriceNet).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.VatRate).IsRequired().HasColumnType("decimal(5,2)");
                
                entity.Ignore(e => e.UnitPriceGross);
                entity.Ignore(e => e.TotalNet);
                entity.Ignore(e => e.VatAmount);
                entity.Ignore(e => e.TotalGross);
                entity.Ignore(e => e.UnitPriceNetFormatted);
                entity.Ignore(e => e.UnitPriceGrossFormatted);
                entity.Ignore(e => e.TotalNetFormatted);
                entity.Ignore(e => e.TotalGrossFormatted);
                entity.Ignore(e => e.VatAmountFormatted);
                
                entity.Property<int>("InvoiceId");
                entity.HasIndex("InvoiceId");
            });

            modelBuilder.Entity<CompanyInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Nip).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Phone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Website).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Regon).HasMaxLength(20);
                entity.Property(e => e.Gln).HasMaxLength(30);
                entity.Property(e => e.DefaultIssuedBy).HasMaxLength(200);
            });

            modelBuilder.Entity<Contractor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Nip).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Regon).HasMaxLength(20);
                entity.Property(e => e.Gln).HasMaxLength(30);
            });

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Items)
                .WithOne()
                .HasForeignKey("InvoiceId")
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}