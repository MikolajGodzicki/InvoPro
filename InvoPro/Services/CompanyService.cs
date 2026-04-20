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
            await EnsureCompanySchemaAsync(context);
            await NormalizeNullCompanyFieldsAsync(context);
            return await context.CompanyInfo.FirstOrDefaultAsync();
        }

        public async Task<CompanyInfo> SaveCompanyInfoAsync(CompanyInfo companyInfo)
        {
            using var context = new InvoiceDbContext();
            await EnsureCompanySchemaAsync(context);
            await NormalizeNullCompanyFieldsAsync(context);
            
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
                    existing.Phone = string.IsNullOrWhiteSpace(companyInfo.Phone) ? string.Empty : companyInfo.Phone;
                    existing.Email = string.IsNullOrWhiteSpace(companyInfo.Email) ? string.Empty : companyInfo.Email;
                    existing.Website = string.IsNullOrWhiteSpace(companyInfo.Website) ? string.Empty : companyInfo.Website;
                    existing.Regon = companyInfo.Regon;
                    existing.Gln = companyInfo.Gln;
                    existing.DefaultIssuedBy = companyInfo.DefaultIssuedBy;
                }

                await context.SaveChangesAsync();
                return companyInfo;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"B³¹d podczas zapisywania danych firmy: {ex.Message}", ex);
            }
        }

        private static async Task NormalizeNullCompanyFieldsAsync(InvoiceDbContext context)
        {
            await context.Database.ExecuteSqlRawAsync(@"
                UPDATE CompanyInfo
                SET
                    Phone = COALESCE(Phone, ''),
                    Email = COALESCE(Email, ''),
                    Website = COALESCE(Website, ''),
                    Regon = COALESCE(Regon, ''),
                    Gln = COALESCE(Gln, ''),
                    DefaultIssuedBy = COALESCE(DefaultIssuedBy, '')
                WHERE
                    Phone IS NULL OR
                    Email IS NULL OR
                    Website IS NULL OR
                    Regon IS NULL OR
                    Gln IS NULL OR
                    DefaultIssuedBy IS NULL;");
        }

        private static async Task EnsureCompanySchemaAsync(InvoiceDbContext context)
        {
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
        }
    }
}