using Microsoft.EntityFrameworkCore;
using InvoPro.Data;
using InvoPro.Models;

namespace InvoPro.Services
{
    public class DatabaseTestService
    {
        public static async Task TestDatabaseAsync()
        {
            try
            {
                Console.WriteLine("=== Test bazy danych ===");
                
                using var context = new InvoiceDbContext();
                
                // SprawdŸ po³¹czenie
                var canConnect = await context.Database.CanConnectAsync();
                Console.WriteLine($"Mo¿na po³¹czyæ: {canConnect}");
                
                // Utwórz bazê jeœli nie istnieje
                if (!canConnect)
                {
                    Console.WriteLine("Tworzenie bazy danych...");
                    await context.Database.EnsureCreatedAsync();
                }
                
                // SprawdŸ ile jest faktur
                var count = await context.Invoices.CountAsync();
                Console.WriteLine($"Liczba faktur: {count}");
                
                // SprawdŸ ponownie
                var newCount = await context.Invoices.CountAsync();
                Console.WriteLine($"Nowa liczba faktur: {newCount}");
                
                Console.WriteLine("=== Test zakoñczony ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"B³¹d testu: {ex}");
            }
        }
    }
}