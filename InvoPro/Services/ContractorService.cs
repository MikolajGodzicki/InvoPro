using InvoPro.Data;
using InvoPro.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoPro.Services
{
    public interface IContractorService
    {
        Task<List<Contractor>> GetAllContractorsAsync();
        Task<Contractor?> GetContractorByNameAsync(string name);
        Task<Contractor> SaveContractorAsync(Contractor contractor);
        Task<bool> DeleteContractorAsync(int id);
    }

    public class ContractorService : IContractorService
    {
        public async Task<List<Contractor>> GetAllContractorsAsync()
        {
            using var context = new InvoiceDbContext();

            return await context.Contractors
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Contractor?> GetContractorByNameAsync(string name)
        {
            using var context = new InvoiceDbContext();

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return await context.Contractors
                .FirstOrDefaultAsync(c => c.Name == name);
        }

        public async Task<Contractor> SaveContractorAsync(Contractor contractor)
        {
            using var context = new InvoiceDbContext();

            if (contractor.Id == 0)
            {
                context.Contractors.Add(contractor);
            }
            else
            {
                var existing = await context.Contractors.FirstOrDefaultAsync(c => c.Id == contractor.Id);
                if (existing == null)
                    throw new InvalidOperationException("Nie znaleziono kontrahenta do aktualizacji.");

                existing.Name = contractor.Name;
                existing.Nip = contractor.Nip;
                existing.Address = contractor.Address;
                existing.Regon = contractor.Regon;
                existing.Gln = contractor.Gln;
            }

            await context.SaveChangesAsync();
            return contractor;
        }

        public async Task<bool> DeleteContractorAsync(int id)
        {
            using var context = new InvoiceDbContext();

            var contractor = await context.Contractors.FirstOrDefaultAsync(c => c.Id == id);
            if (contractor == null)
                return false;

            context.Contractors.Remove(contractor);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
