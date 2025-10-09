using Microsoft.EntityFrameworkCore;
using InvoPro.Data;
using InvoPro.Models;

namespace InvoPro.Services
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
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
            
            System.Diagnostics.Debug.WriteLine("Pobieranie faktur z bazy danych...");
            var invoices = await context.Invoices
                .Include(i => i.Items)
                .OrderByDescending(i => i.IssueDate)
                .ToListAsync();
                
            System.Diagnostics.Debug.WriteLine($"Pobrano {invoices.Count} faktur z bazy danych.");
            return invoices;
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
                System.Diagnostics.Debug.WriteLine($"Zapisywanie faktury: {invoice.Number}, ID: {invoice.Id}");
                
                if (invoice.Id == 0)
                {
                    // Nowa faktura
                    System.Diagnostics.Debug.WriteLine("Dodawanie nowej faktury...");
                    
                    foreach (var item in invoice.Items)
                    {
                        item.Id = 0; // Wymuœ utworzenie nowego ID
                    }
                    
                    context.Invoices.Add(invoice);
                }
                else
                {
                    // Aktualizacja istniej¹cej faktury
                    System.Diagnostics.Debug.WriteLine($"Aktualizacja faktury ID: {invoice.Id}");
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

                System.Diagnostics.Debug.WriteLine("Wywo³anie SaveChangesAsync...");
                var changes = await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Zapisano {changes} zmian.");
                
                // Zwróæ fakturê z baz¹ z nowymi ID
                var savedInvoice = await context.Invoices
                    .Include(i => i.Items)
                    .FirstAsync(i => i.Number == invoice.Number);
                    
                System.Diagnostics.Debug.WriteLine($"Faktura zapisana z ID: {savedInvoice.Id}");
                return savedInvoice;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"B³¹d podczas zapisywania faktury: {ex}");
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
                System.Diagnostics.Debug.WriteLine("Inicjalizacja bazy danych...");
                
                // SprawdŸ czy baza danych istnieje i zastosuj migracje
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                
                System.Diagnostics.Debug.WriteLine($"Zastosowane migracje: {appliedMigrations.Count()}");
                System.Diagnostics.Debug.WriteLine($"Penduj¹ce migracje: {pendingMigrations.Count()}");
                
                if (pendingMigrations.Any())
                {
                    System.Diagnostics.Debug.WriteLine("Stosowanie migracji...");
                    await context.Database.MigrateAsync();
                    System.Diagnostics.Debug.WriteLine("Migracje zastosowane pomyœlnie.");
                }
                else if (!appliedMigrations.Any())
                {
                    // Jeœli nie ma ¿adnych migracji, utwórz bazê
                    System.Diagnostics.Debug.WriteLine("Brak migracji. Tworzenie bazy danych...");
                    await context.Database.EnsureCreatedAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Baza danych jest aktualna.");
                }
                
                // SprawdŸ czy mo¿na po³¹czyæ siê z baz¹
                var canConnect = await context.Database.CanConnectAsync();
                System.Diagnostics.Debug.WriteLine($"Po³¹czenie z baz¹: {canConnect}");
                
                if (canConnect)
                {
                    // SprawdŸ czy baza ma jakieœ dane
                    var invoiceCount = await context.Invoices.CountAsync();
                    System.Diagnostics.Debug.WriteLine($"Liczba faktur w bazie: {invoiceCount}");
                }
                
                System.Diagnostics.Debug.WriteLine("Inicjalizacja bazy danych zakoñczona pomyœlnie.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"B³¹d inicjalizacji bazy danych: {ex}");
                throw new InvalidOperationException($"B³¹d podczas inicjalizacji bazy danych: {ex.Message}", ex);
            }
        }

        public async Task ResetDatabaseAsync()
        {
            using var context = new InvoiceDbContext();
            
            try
            {
                System.Diagnostics.Debug.WriteLine("Resetowanie bazy danych...");
                
                // Usuñ bazê danych
                await context.Database.EnsureDeletedAsync();
                System.Diagnostics.Debug.WriteLine("Baza danych usuniêta.");
                
                // Zastosuj migracje
                await context.Database.MigrateAsync();
                System.Diagnostics.Debug.WriteLine("Migracje zastosowane.");
                
                System.Diagnostics.Debug.WriteLine("Reset bazy danych zakoñczony pomyœlnie.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"B³¹d podczas resetowania bazy danych: {ex}");
                throw;
            }
        }
    }
}