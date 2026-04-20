using Microsoft.EntityFrameworkCore;
using InvoPro.Data;
using InvoPro.Models;

namespace InvoPro.Services
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<List<string>> GetContractorNamesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice> SaveInvoiceAsync(Invoice invoice);
        Task<bool> DeleteInvoiceAsync(int id);
        Task InitializeDatabaseAsync();
        Task ResetDatabaseAsync();
    }

    public class InvoiceService : IInvoiceService
    {
        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            using var context = new InvoiceDbContext();
            
            var invoices = await context.Invoices
                .Include(i => i.Items)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
                
            return invoices;
        }

        public async Task<List<string>> GetContractorNamesAsync()
        {
            using var context = new InvoiceDbContext();

            return await context.Invoices
                .Where(i => !string.IsNullOrWhiteSpace(i.ClientName))
                .Select(i => i.ClientName)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            using var context = new InvoiceDbContext();
            return await context.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Invoice> SaveInvoiceAsync(Invoice invoice)
        {
            using var context = new InvoiceDbContext();
            
            try
            {
                if (invoice.Id == 0)
                {
                    // Nowa faktura
                    foreach (var item in invoice.Items)
                    {
                        item.Id = 0; // Wymuœ utworzenie nowego ID
                    }
                    
                    context.Invoices.Add(invoice);
                }
                else
                {
                    // Aktualizacja istniej¹cej faktury
                    var existingInvoice = await context.Invoices
                        .Include(i => i.Items)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id);

                    if (existingInvoice == null)
                        throw new InvalidOperationException($"Faktura o ID {invoice.Id} nie zosta³a znaleziona.");

                    // Aktualizuj podstawowe w³aœciwoœci
                    existingInvoice.Number = invoice.Number;
                    existingInvoice.IssueDate = invoice.IssueDate;
                    existingInvoice.DueDate = invoice.DueDate;
                    existingInvoice.ClientName = invoice.ClientName;
                    existingInvoice.ClientAddress = invoice.ClientAddress;
                    existingInvoice.ClientNip = invoice.ClientNip;
                    existingInvoice.Description = invoice.Description;

                    // Usuñ stare pozycje
                    context.InvoiceItems.RemoveRange(existingInvoice.Items);

                    // Dodaj nowe pozycje (z resetowanymi ID)
                    foreach (var item in invoice.Items)
                    {
                        var newItem = new InvoiceItem
                        {
                            Id = 0, // Wymuœ utworzenie nowego ID
                            Name = item.Name,
                            Description = item.Description,
                            Quantity = item.Quantity,
                            Unit = item.Unit,
                            UnitPriceNet = item.UnitPriceNet,
                            DiscountPercentage = item.DiscountPercentage,
                            VatRate = item.VatRate
                        };
                        existingInvoice.Items.Add(newItem);
                    }
                }

                var changes = await context.SaveChangesAsync();
                
                // Zwróæ fakturê z baz¹ z nowymi ID
                var savedInvoice = await context.Invoices
                    .Include(i => i.Items)
                    .FirstAsync(i => i.Number == invoice.Number);
                    
                return savedInvoice;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"B³¹d podczas zapisywania faktury: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            using var context = new InvoiceDbContext();
            
            var invoice = await context.Invoices.FindAsync(id);
            if (invoice == null)
                return false;

            context.Invoices.Remove(invoice);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task InitializeDatabaseAsync()
        {
            using var context = new InvoiceDbContext();
            
            try
            {
                // SprawdŸ czy baza danych istnieje i zastosuj migracje
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    await context.Database.MigrateAsync();
                }
                else if (!appliedMigrations.Any())
                {
                    // Jeœli nie ma ¿adnych migracji, utwórz bazê
                    await context.Database.EnsureCreatedAsync();
                }
                
                // SprawdŸ czy mo¿na po³¹czyæ siê z baz¹
                var canConnect = await context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    // SprawdŸ czy baza ma jakieœ dane
                    var invoiceCount = await context.Invoices.CountAsync();

                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS Contractors (
                            Id INTEGER NOT NULL CONSTRAINT PK_Contractors PRIMARY KEY AUTOINCREMENT,
                            Name TEXT NOT NULL,
                            Nip TEXT NULL,
                            Address TEXT NULL,
                            Regon TEXT NULL,
                            Gln TEXT NULL
                        );");

                    try
                    {
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE CompanyInfo ADD COLUMN DefaultIssuedBy TEXT NULL;");
                    }
                    catch
                    {
                    }

                    try
                    {
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE CompanyInfo ADD COLUMN Regon TEXT NULL;");
                    }
                    catch
                    {
                    }

                    try
                    {
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE CompanyInfo ADD COLUMN Gln TEXT NULL;");
                    }
                    catch
                    {
                    }

                    try
                    {
                        await context.Database.ExecuteSqlRawAsync("ALTER TABLE Invoices ADD COLUMN ShowNetPrices INTEGER NOT NULL DEFAULT 0;");
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"B³¹d podczas inicjalizacji bazy danych: {ex.Message}", ex);
            }
        }

        public async Task ResetDatabaseAsync()
        {
            using var context = new InvoiceDbContext();
            
            try
            {
                // Usuñ bazê danych
                await context.Database.EnsureDeletedAsync();
                
                // Zastosuj migracje
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}