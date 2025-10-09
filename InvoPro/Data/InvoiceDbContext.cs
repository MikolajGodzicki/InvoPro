using Microsoft.EntityFrameworkCore;
using InvoPro.Models;
using System.IO;

namespace InvoPro.Data
{
    public class InvoiceDbContext : DbContext
    {
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<CompanyInfo> CompanyInfo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Œcie¿ka do bazy danych w folderze aplikacji
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InvoPro", "invoices.db");
            
            // Utwórz folder jeœli nie istnieje
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
            // Dodaj informacjê o lokalizacji bazy danych do debugowania
            System.Diagnostics.Debug.WriteLine($"Baza danych SQLite: {dbPath}");
            Console.WriteLine($"Baza danych SQLite: {dbPath}");
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfiguracja modelu Invoice
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
                
                // W³aœciwoœci obliczane nie s¹ przechowywane w bazie danych
                entity.Ignore(e => e.TotalNet);
                entity.Ignore(e => e.TotalVat);
                entity.Ignore(e => e.TotalAmount);
                entity.Ignore(e => e.TotalNetFormatted);
                entity.Ignore(e => e.TotalVatFormatted);
                entity.Ignore(e => e.TotalAmountFormatted);
            });

            // Konfiguracja modelu InvoiceItem
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
                
                // W³aœciwoœci obliczane nie s¹ przechowywane w bazie danych
                entity.Ignore(e => e.UnitPriceGross);
                entity.Ignore(e => e.TotalNet);
                entity.Ignore(e => e.VatAmount);
                entity.Ignore(e => e.TotalGross);
                entity.Ignore(e => e.UnitPriceNetFormatted);
                entity.Ignore(e => e.UnitPriceGrossFormatted);
                entity.Ignore(e => e.TotalNetFormatted);
                entity.Ignore(e => e.TotalGrossFormatted);
                entity.Ignore(e => e.VatAmountFormatted);
                
                // Dodaj kolumnê InvoiceId jako klucz obcy z relacj¹
                entity.Property<int>("InvoiceId");
                entity.HasIndex("InvoiceId");
            });

            // Konfiguracja modelu CompanyInfo
            modelBuilder.Entity<CompanyInfo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Nip).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Website).HasMaxLength(100);
            });

            // Konfiguracja relacji
            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.Items)
                .WithOne()
                .HasForeignKey("InvoiceId")
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}