using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using InvoPro.Models;
using InvoPro.Services;
using System.IO;

namespace InvoPro.Services
{
    public interface IPdfService
    {
        Task<string> GenerateInvoicePdfAsync(Invoice invoice, string? saveDirectory = null);
        Task OpenPdfAsync(string filePath);
    }

    public class PdfService : IPdfService
    {
        private readonly ICompanyService _companyService;

        public PdfService()
        {
            _companyService = new CompanyService();
        }

        public async Task<string> GenerateInvoicePdfAsync(Invoice invoice, string? saveDirectory = null)
        {
            try
            {
                // SprawdŸ czy invoice nie jest null
                if (invoice == null)
                    throw new ArgumentNullException(nameof(invoice), "Faktura nie mo¿e byæ null");
                
                // Okreœl katalog zapisu
                if (string.IsNullOrEmpty(saveDirectory))
                {
                    saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }

                // Upewnij siê, ¿e katalog istnieje
                Directory.CreateDirectory(saveDirectory);
                
                // Nazwa pliku z bezpiecznymi znakami
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
                
                var fileName = $"Faktura_{safeInvoiceNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(saveDirectory, fileName);

                // SprawdŸ czy plik nie istnieje ju¿
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        // Spróbuj inn¹ nazwê
                        fileName = $"Faktura_{safeInvoiceNumber}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.pdf";
                        filePath = Path.Combine(saveDirectory, fileName);
                    }
                }

                // Pobierz dane firmy
                CompanyInfo? companyInfo = null;
                try
                {
                    companyInfo = await _companyService.GetCompanyInfoAsync();
                }
                catch (Exception ex)
                {
                    // Kontynuuj bez danych firmy
                }

                // Utwórz w³aœciwy PDF
                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // POLSKIE CZCIONKI - obs³uga znaków diakrytycznych
                PdfFont boldFont, regularFont;
                try
                {
                    // Lista mo¿liwych œcie¿ek do czcionek Arial
                    var arialPaths = new[]
                    {
                        @"c:\windows\fonts\arial.ttf",
                        @"C:\Windows\Fonts\arial.ttf", 
                        @"C:\Windows\Fonts\Arial.ttf",
                        @"/System/Library/Fonts/Arial.ttf", // macOS
                        @"/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf" // Linux
                    };

                    string? workingArialPath = null;
                    foreach (var path in arialPaths)
                    {
                        if (File.Exists(path))
                        {
                            workingArialPath = path;
                            break;
                        }
                    }

                    if (workingArialPath != null)
                    {
                        // U¿yj czcionki systemowej z obs³ug¹ polskich znaków
                        boldFont = PdfFontFactory.CreateFont(workingArialPath, "Cp1250", PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                        regularFont = PdfFontFactory.CreateFont(workingArialPath, "Cp1250", PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                    else
                    {
                        throw new FileNotFoundException("Nie znaleziono czcionek Arial");
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        // Fallback - spróbuj Times z kodowaniem
                        boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD, "Cp1250");
                        regularFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN, "Cp1250");
                    }
                    catch (Exception ex2)
                    {
                        try
                        {
                            // Fallback - próba z UTF-8
                            boldFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_BOLD, "UTF-8");
                            regularFont = PdfFontFactory.CreateFont(StandardFonts.TIMES_ROMAN, "UTF-8");
                        }
                        catch
                        {
                            // Last resort - standardowe czcionki bez kodowania
                            boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                            regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        }
                    }
                }

                // === NAG£ÓWEK DOKUMENTU ===
                var title = new Paragraph("Faktura VAT ")
                    .SetFont(boldFont)
                    .SetFontSize(24)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(title);

                // === TABELA Z DANYMI FIRMY I KLIENTA ===
                var headerTable = new Table(2, false);
                headerTable.SetWidth(UnitValue.CreatePercentValue(100));

                // Kolumna 1: Dane sprzedawcy
                var sellerCell = new Cell();
                sellerCell.Add(new Paragraph("SPRZEDAWCA").SetFont(boldFont).SetFontSize(12));
                if (companyInfo != null)
                {
                    sellerCell.Add(new Paragraph(companyInfo.Name ?? "").SetFont(boldFont));
                    sellerCell.Add(new Paragraph(companyInfo.Address ?? "").SetFont(regularFont));
                    sellerCell.Add(new Paragraph($"NIP: {companyInfo.Nip ?? ""}").SetFont(regularFont));
                    if (!string.IsNullOrEmpty(companyInfo.Phone))
                        sellerCell.Add(new Paragraph($"Tel: {companyInfo.Phone}").SetFont(regularFont));
                    if (!string.IsNullOrEmpty(companyInfo.Email))
                        sellerCell.Add(new Paragraph($"Email: {companyInfo.Email}").SetFont(regularFont));
                }
                else
                {
                    sellerCell.Add(new Paragraph("Dane firmy nie zosta³y ustawione").SetFont(regularFont));
                }
                sellerCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                // Kolumna 2: Dane nabywcy
                var buyerCell = new Cell();
                buyerCell.Add(new Paragraph("NABYWCA").SetFont(boldFont).SetFontSize(12));
                buyerCell.Add(new Paragraph(invoice.ClientName ?? "").SetFont(boldFont));
                buyerCell.Add(new Paragraph(invoice.ClientAddress ?? "").SetFont(regularFont));
                buyerCell.Add(new Paragraph($"NIP: {invoice.ClientNip ?? ""}").SetFont(regularFont));
                buyerCell.SetBorder(iText.Layout.Borders.Border.NO_BORDER);

                headerTable.AddCell(sellerCell);
                headerTable.AddCell(buyerCell);
                document.Add(headerTable);

                // === DANE FAKTURY ===
                document.Add(new Paragraph("\n"));
                var invoiceInfoTable = new Table(4, false);
                invoiceInfoTable.SetWidth(UnitValue.CreatePercentValue(100));

                invoiceInfoTable.AddCell(CreateInfoCell("Numer faktury:", boldFont));
                invoiceInfoTable.AddCell(CreateInfoCell(invoice.Number ?? "", regularFont));
                invoiceInfoTable.AddCell(CreateInfoCell("Data wystawienia:", boldFont));
                invoiceInfoTable.AddCell(CreateInfoCell(invoice.IssueDate.ToString("dd.MM.yyyy"), regularFont));

                invoiceInfoTable.AddCell(CreateInfoCell("Miejsce wystawienia:", boldFont));
                var place = "Warszawa";
                if (companyInfo?.Address != null)
                {
                    var addressParts = companyInfo.Address.Split(',');
                    if (addressParts.Length > 0)
                        place = addressParts[^1].Trim();
                }
                invoiceInfoTable.AddCell(CreateInfoCell(place, regularFont));
                invoiceInfoTable.AddCell(CreateInfoCell("Termin p³atnoœci:", boldFont));
                invoiceInfoTable.AddCell(CreateInfoCell(invoice.DueDate.ToString("dd.MM.yyyy"), regularFont));

                document.Add(invoiceInfoTable);

                // === TABELA POZYCJI ===
                if (invoice.Items?.Count > 0)
                {
                    document.Add(new Paragraph("\n"));
                    var itemsTable = new Table(7, false);
                    itemsTable.SetWidth(UnitValue.CreatePercentValue(100));

                    // Nag³ówki kolumn
                    itemsTable.AddHeaderCell(CreateHeaderCell("Lp.", boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Nazwa towaru/us³ugi", boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Iloœæ", boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Jedn.", boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Cena netto", boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("VAT %", boldFont));
                    itemsTable.AddHeaderCell(CreateHeaderCell("Wartoœæ brutto", boldFont));

                    // Pozycje
                    int lp = 1;
                    foreach (var item in invoice.Items)
                    {
                        itemsTable.AddCell(CreateCell(lp.ToString(), regularFont));
                        itemsTable.AddCell(CreateCell(item.Name ?? "", regularFont));
                        itemsTable.AddCell(CreateCell(item.Quantity.ToString("F2"), regularFont));
                        itemsTable.AddCell(CreateCell(item.Unit ?? "", regularFont));
                        itemsTable.AddCell(CreateCell($"{item.UnitPriceNet:F2} PLN", regularFont));
                        itemsTable.AddCell(CreateCell($"{item.VatRate:F0}%", regularFont));
                        itemsTable.AddCell(CreateCell($"{item.TotalGross:F2} PLN", regularFont));
                        lp++;
                    }

                    document.Add(itemsTable);
                }

                // === PODSUMOWANIE ===
                document.Add(new Paragraph("\n"));
                var summaryTable = new Table(2, false);
                summaryTable.SetWidth(UnitValue.CreatePercentValue(50));
                summaryTable.SetHorizontalAlignment(HorizontalAlignment.RIGHT);

                summaryTable.AddCell(CreateSummaryCell("Wartoœæ netto:", boldFont));
                summaryTable.AddCell(CreateSummaryCell($"{invoice.TotalNet:F2} PLN", regularFont));

                summaryTable.AddCell(CreateSummaryCell("VAT:", boldFont));
                summaryTable.AddCell(CreateSummaryCell($"{invoice.TotalVat:F2} PLN", regularFont));

                summaryTable.AddCell(CreateSummaryCell("RAZEM DO ZAP£ATY:", boldFont));
                summaryTable.AddCell(CreateSummaryCell($"{invoice.TotalAmount:F2} PLN", boldFont));

                document.Add(summaryTable);

                // === UWAGI ===
                if (!string.IsNullOrEmpty(invoice.Description))
                {
                    document.Add(new Paragraph("\n"));
                    document.Add(new Paragraph("Uwagi:").SetFont(boldFont));
                    document.Add(new Paragraph(invoice.Description).SetFont(regularFont));
                }

                // === STOPKA ===
                document.Add(new Paragraph("\n\n"));
                document.Add(new Paragraph($"Dokument wygenerowany: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.GRAY));

                return filePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Nie uda³o siê wygenerowaæ PDF: {ex.Message}", ex);
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
                    throw new InvalidOperationException($"Nie mo¿na otworzyæ pliku PDF: {ex.Message}");
                }
            });
        }

        private Cell CreateHeaderCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text))
                .SetFont(font)
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5);
        }

        private Cell CreateCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text))
                .SetFont(font)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(3);
        }

        private Cell CreateInfoCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text))
                .SetFont(font)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(2);
        }

        private Cell CreateSummaryCell(string text, PdfFont font)
        {
            return new Cell()
                .Add(new Paragraph(text))
                .SetFont(font)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(3)
                .SetTextAlignment(TextAlignment.RIGHT);
        }
    }
}