using InvoPro.Models;
using InvoPro.Services;
using System.IO;
using System.Text;

namespace InvoPro.Services
{
    public class HtmlToPdfService : IPdfService
    {
        private readonly ICompanyService _companyService;

        public HtmlToPdfService()
        {
            _companyService = new CompanyService();
        }

        public async Task<string> GenerateInvoicePdfAsync(Invoice invoice, string? saveDirectory = null)
        {
            try
            {
                // Okreœl katalog zapisu
                if (string.IsNullOrEmpty(saveDirectory))
                {
                    saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                Directory.CreateDirectory(saveDirectory);

                // Nazwa pliku
                var safeInvoiceNumber = (invoice.Number ?? "BRAK")
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Replace("*", "_")
                    .Replace("?", "_")
                    .Replace("\"", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_");

                var fileName = $"Faktura_{safeInvoiceNumber}.html";
                var filePath = Path.Combine(saveDirectory, fileName);

                // Pobierz dane firmy
                var companyInfo = await _companyService.GetCompanyInfoAsync();

                // Generuj HTML
                var html = await GenerateInvoiceHtmlAsync(invoice, companyInfo);

                // Zapisz HTML do pliku
                await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

                return filePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Nie uda³o siê wygenerowaæ HTML: {ex.Message}", ex);
            }
        }

        public async Task OpenPdfAsync(string filePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Nie mo¿na otworzyæ pliku: {ex.Message}");
                }
            });
        }

        private async Task<string> GenerateInvoiceHtmlAsync(Invoice invoice, CompanyInfo? companyInfo)
        {
            return await Task.FromResult($@"
<!DOCTYPE html>
<html lang='pl'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Faktura {invoice.Number}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, Arial, sans-serif;
            margin: 20px;
            color: #333;
            font-size: 12px;
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .header h1 {{
            font-size: 28px;
            margin: 0;
            color: #2c3e50;
        }}
        .company-info {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 30px;
            margin-bottom: 30px;
        }}
        .company-section {{
            border: 1px solid #ddd;
            padding: 15px;
            border-radius: 5px;
        }}
        .company-section h3 {{
            margin-top: 0;
            color: #2c3e50;
            border-bottom: 2px solid #3498db;
            padding-bottom: 5px;
        }}
        .invoice-details {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-bottom: 30px;
            padding: 15px;
            background-color: #f8f9fa;
            border-radius: 5px;
        }}
        .items-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 30px;
            font-size: 11px;
        }}
        .items-table th,
        .items-table td {{
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }}
        .items-table th {{
            background-color: #34495e;
            color: white;
            font-weight: bold;
        }}
        .items-table tr:nth-child(even) {{
            background-color: #f2f2f2;
        }}
        .items-table td.number {{
            text-align: right;
        }}
        .summary {{
            float: right;
            width: 300px;
            border: 2px solid #2c3e50;
            padding: 15px;
            border-radius: 5px;
            background-color: #ecf0f1;
        }}
        .summary-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
        }}
        .total {{
            font-weight: bold;
            font-size: 16px;
            border-top: 2px solid #2c3e50;
            padding-top: 10px;
        }}
        .notes {{
            clear: both;
            margin-top: 30px;
            padding: 15px;
            background-color: #f8f9fa;
            border-radius: 5px;
        }}
        .footer {{
            text-align: center;
            margin-top: 50px;
            font-size: 10px;
            color: #7f8c8d;
        }}
        @media print {{
            body {{ margin: 0; font-size: 11px; }}
            .no-print {{ display: none; }}
        }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>Faktura VAT</h1>
    </div>

    <div class='company-info'>
        <div class='company-section'>
            <h3>SPRZEDAWCA</h3>
            {(companyInfo != null ? $@"
            <strong>{companyInfo.Name}</strong><br>
            {companyInfo.Address}<br> 
            NIP: {companyInfo.Nip}<br>
            {(!string.IsNullOrEmpty(companyInfo.Phone) ? $"Tel: {companyInfo.Phone}<br>" : "")}
            {(!string.IsNullOrEmpty(companyInfo.Email) ? $"Email: {companyInfo.Email}<br>" : "")}
            " : "Dane firmy nie zosta³y ustawione")}
        </div>
        <div class='company-section'>
            <h3>NABYWCA</h3>
            <strong>{invoice.ClientName}</strong><br>
            {invoice.ClientAddress}<br>
            NIP: {invoice.ClientNip}
        </div>
    </div>

    <div class='invoice-details'>
        <div>
            <strong>Numer faktury:</strong> {invoice.Number}<br>
            <strong>Miejsce wystawienia:</strong> {(companyInfo?.Address?.Split(',').LastOrDefault()?.Trim() ?? "Warszawa")}
        </div>
        <div>
            <strong>Data wystawienia:</strong> {invoice.IssueDate:dd.MM.yyyy}<br>
            <strong>Termin p³atnoœci:</strong> {invoice.DueDate:dd.MM.yyyy}
        </div>
    </div>

    {(invoice.Items?.Count > 0 ? $@"
    <table class='items-table'>
        <thead>
            <tr>
                <th style='width: 30px;'>Lp.</th>
                <th style='width: 200px;'>Nazwa towaru/us³ugi</th>
                <th style='width: 60px;'>Iloœæ</th>
                <th style='width: 50px;'>Jedn.</th>
                <th style='width: 80px;'>Cena netto (PLN)</th>
                <th style='width: 60px;'>VAT %</th>
                <th style='width: 100px;'>Wartoœæ brutto (PLN)</th>
            </tr>
        </thead>
        <tbody>
            {string.Join("", invoice.Items.Select((item, index) => $@"
            <tr>
                <td style='text-align: center;'>{index + 1}</td>
                <td>{item.Name}</td>
                <td class='number'>{item.Quantity:F2}</td>
                <td style='text-align: center;'>{item.Unit}</td>
                <td class='number'>{item.UnitPriceNet:F2}</td>
                <td style='text-align: center;'>{item.VatRate:F0}%</td>
                <td class='number'>{item.TotalGross:F2}</td>
            </tr>"))}
        </tbody>
    </table>" : "")}

    <div class='summary'>
        <div class='summary-row'>
            <span>Wartoœæ netto:</span>
            <span>{invoice.TotalNet:F2} PLN</span>
        </div>
        <div class='summary-row'>
            <span>VAT:</span>
            <span>{invoice.TotalVat:F2} PLN</span>
        </div>
        <div class='summary-row total'>
            <span>RAZEM DO ZAP£ATY:</span>
            <span>{invoice.TotalAmount:F2} PLN</span>
        </div>
    </div>

    {(!string.IsNullOrEmpty(invoice.Description) ? $@"
    <div class='notes'>
        <h3>Uwagi:</h3>
        <p>{invoice.Description}</p>
    </div>" : "")}

    <div class='footer'>
        <p>Dokument wygenerowany: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
        <p class='no-print'>Aby wydrukowaæ lub zapisaæ jako PDF, u¿yj funkcji drukowania przegl¹darki (Ctrl+P)</p>
    </div>
</body>
</html>");
        }
    }
}