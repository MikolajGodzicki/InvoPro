using Microsoft.EntityFrameworkCore;
using InvoPro.Data;
using InvoPro.Models;

namespace InvoPro.Services
{
    public interface ICompanyService
    {
        Task<CompanyInfo?> GetCompanyInfoAsync();
        Task<CompanyInfo> SaveCompanyInfoAsync(CompanyInfo companyInfo);
    }

    public class CompanyService : ICompanyService
    {
        public async Task<CompanyInfo?> GetCompanyInfoAsync()
        {
            using var context = new InvoiceDbContext();
            return await context.CompanyInfo.FirstOrDefaultAsync();
        }

        public async Task<CompanyInfo> SaveCompanyInfoAsync(CompanyInfo companyInfo)
        {
            using var context = new InvoiceDbContext();
            
            try
            {
                var existing = await context.CompanyInfo.FirstOrDefaultAsync();
                
                if (existing == null)
                {
                    // Nowe dane firmy
                    context.CompanyInfo.Add(companyInfo);
                }
                else
                {
                    // Aktualizacja istniej¹cych danych
                    existing.Name = companyInfo.Name;
                    existing.Address = companyInfo.Address;
                    existing.Nip = companyInfo.Nip;
                    existing.Phone = companyInfo.Phone;
                    existing.Email = companyInfo.Email;
                    existing.Website = companyInfo.Website;
                }

                await context.SaveChangesAsync();
                return companyInfo;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"B³¹d podczas zapisywania danych firmy: {ex.Message}", ex);
            }
        }
    }
}