using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Monolithic.Api.Modules.Finance.Contracts;
using Monolithic.Api.Modules.Finance.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Monolithic.Api.Modules.Finance.Application;

/// <summary>
/// Exports <see cref="FinancialReportDto"/> to CSV, Excel (.xlsx), or PDF.
///
/// DRY design: all three formats share the same traversal order:
///   Header → [Section header → Account rows → Section total]* → Summary → Exchange rates
/// Format-specific helpers handle cell/paragraph creation while the traversal stays centralised.
/// </summary>
public sealed class ReportExportService : IReportExportService
{
    // Licence declaration required once per process for QuestPDF Community Edition.
    static ReportExportService() => QuestPDF.Settings.License = LicenseType.Community;

    public Task<(byte[] Data, string ContentType, string FileName)> ExportAsync(
        FinancialReportDto report,
        ExportFormat format,
        CancellationToken ct = default)
    {
        var result = format switch
        {
            ExportFormat.Csv => ExportToCsv(report),
            ExportFormat.Excel => ExportToExcel(report),
            ExportFormat.Pdf => ExportToPdf(report),
            _ => throw new ArgumentOutOfRangeException(nameof(format), $"Unsupported format: {format}")
        };

        return Task.FromResult(result);
    }

    // ── CSV ───────────────────────────────────────────────────────────────────

    private static (byte[] Data, string ContentType, string FileName) ExportToCsv(FinancialReportDto report)
    {
        var sb = new StringBuilder();
        var currency = report.ReportingCurrencyCode;

        // ── File header ───────────────────────────────────────────────────────
        AppendCsvLine(sb, report.ReportTitle);
        AppendCsvLine(sb, $"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}");
        AppendCsvLine(sb, $"Reporting Currency: {currency}");
        AppendCsvLine(sb, $"Translation Type: {report.TranslationType}");
        AppendCsvLine(sb, $"Businesses: {string.Join("; ", report.BusinessNames)}");
        AppendCsvLine(sb, $"Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // ── Column headers ────────────────────────────────────────────────────
        AppendCsvRow(sb, "Account #", "Account Name", "Type", "Category",
            "Debits (Base)", "Credits (Base)", "Balance (Base)",
            $"Debits ({currency})", $"Credits ({currency})", $"Balance ({currency})",
            "Exchange Rate");

        // ── Sections ──────────────────────────────────────────────────────────
        foreach (var section in report.Sections)
        {
            sb.AppendLine();
            AppendCsvLine(sb, $"── {section.SectionName.ToUpperInvariant()} ──");

            foreach (var line in section.Lines)
                AppendCsvRow(sb,
                    line.AccountNumber, line.AccountName, line.AccountType, line.AccountCategory,
                    FormatDecimal(line.TotalDebits), FormatDecimal(line.TotalCredits), FormatDecimal(line.Balance),
                    FormatDecimal(line.TranslatedDebits), FormatDecimal(line.TranslatedCredits), FormatDecimal(line.TranslatedBalance),
                    FormatDecimal(line.ExchangeRateApplied, 6));

            AppendCsvRow(sb, "", $"TOTAL {section.SectionName.ToUpperInvariant()}", "", "",
                FormatDecimal(section.SectionTotalDebits), FormatDecimal(section.SectionTotalCredits), FormatDecimal(section.SectionBalance),
                FormatDecimal(section.TranslatedSectionTotalDebits), FormatDecimal(section.TranslatedSectionTotalCredits), FormatDecimal(section.TranslatedSectionBalance),
                "");
        }

        // ── Summary ───────────────────────────────────────────────────────────
        sb.AppendLine();
        AppendCsvLine(sb, "── SUMMARY ──");
        AppendSummaryTocsv(sb, report, currency);

        // ── Exchange rates used ───────────────────────────────────────────────
        sb.AppendLine();
        AppendCsvLine(sb, "── EXCHANGE RATES APPLIED ──");
        AppendCsvRow(sb, "From", "To", "Rate", "Type", "As Of Date");
        foreach (var rate in report.ExchangeRatesUsed)
            AppendCsvRow(sb, rate.FromCurrency, rate.ToCurrency,
                FormatDecimal(rate.RateUsed, 6), rate.TranslationType.ToString(),
                rate.RateAsOfDate?.ToString("yyyy-MM-dd") ?? "Period Average");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = BuildFileName(report, "csv");
        return (bytes, "text/csv; charset=utf-8", fileName);
    }

    private static void AppendCsvLine(StringBuilder sb, string value)
        => sb.AppendLine(QuoteCsvValue(value));

    private static void AppendCsvRow(StringBuilder sb, params string[] values)
        => sb.AppendLine(string.Join(",", values.Select(QuoteCsvValue)));

    private static string QuoteCsvValue(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static void AppendSummaryTocsv(StringBuilder sb, FinancialReportDto report, string currency)
    {
        if (report.NetIncome.HasValue)
        {
            AppendCsvRow(sb, "Total Revenue", FormatDecimal(report.TotalRevenue));
            AppendCsvRow(sb, "Total Expenses", FormatDecimal(report.TotalExpenses));
            AppendCsvRow(sb, $"Net Income ({currency})", FormatDecimal(report.NetIncome));
        }

        if (report.TotalAssets.HasValue)
        {
            AppendCsvRow(sb, $"Total Assets ({currency})", FormatDecimal(report.TotalAssets));
            AppendCsvRow(sb, $"Total Liabilities ({currency})", FormatDecimal(report.TotalLiabilities));
            AppendCsvRow(sb, $"Total Equity ({currency})", FormatDecimal(report.TotalEquity));
            AppendCsvRow(sb, $"Liabilities + Equity ({currency})", FormatDecimal(report.TotalLiabilitiesAndEquity));
            AppendCsvRow(sb, "Balanced?", (report.IsBalanced == true ? "Yes" : "No"));
        }

        if (report.TrialBalanceTotalDebits.HasValue)
        {
            AppendCsvRow(sb, $"Total Debits ({currency})", FormatDecimal(report.TrialBalanceTotalDebits));
            AppendCsvRow(sb, $"Total Credits ({currency})", FormatDecimal(report.TrialBalanceTotalCredits));
        }
    }

    // ── Excel ─────────────────────────────────────────────────────────────────

    private static (byte[] Data, string ContentType, string FileName) ExportToExcel(FinancialReportDto report)
    {
        using var workbook = new XLWorkbook();
        var currency = report.ReportingCurrencyCode;

        // ── Main data sheet ───────────────────────────────────────────────────
        var ws = workbook.Worksheets.Add(TruncateSheetName(report.ReportType.ToString()));
        var row = 1;

        // Metadata block
        WriteExcelCell(ws, row++, 1, report.ReportTitle, bold: true, fontSize: 14);
        WriteExcelCell(ws, row++, 1, $"Period: {report.FromDate:yyyy-MM-dd} to {report.ToDate:yyyy-MM-dd}");
        WriteExcelCell(ws, row++, 1, $"Reporting Currency: {currency}");
        WriteExcelCell(ws, row++, 1, $"Translation: {report.TranslationType}  |  Consolidation: {report.ConsolidationLevel}");
        WriteExcelCell(ws, row++, 1, $"Entities: {string.Join(", ", report.BusinessNames)}");
        WriteExcelCell(ws, row++, 1, $"Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        row++;

        // Column headers
        string[] headers =
        [
            "Account #", "Account Name", "Type", "Category",
            "Debits (Base)", "Credits (Base)", "Balance (Base)",
            $"Debits ({currency})", $"Credits ({currency})", $"Balance ({currency})",
            "Exchange Rate"
        ];

        for (var col = 1; col <= headers.Length; col++)
            WriteExcelCell(ws, row, col, headers[col - 1], bold: true, bgColor: XLColor.FromHtml("#1E40AF"), fontColor: XLColor.White);
        row++;

        // Data rows
        foreach (var section in report.Sections)
        {
            // Section header
            for (var col = 1; col <= headers.Length; col++)
                WriteExcelCell(ws, row, col, col == 1 ? section.SectionName.ToUpperInvariant() : "",
                    bold: true, bgColor: XLColor.FromHtml("#DBEAFE"));
            row++;

            foreach (var line in section.Lines)
            {
                ws.Cell(row, 1).Value = line.AccountNumber;
                ws.Cell(row, 2).Value = line.AccountName;
                ws.Cell(row, 3).Value = line.AccountType;
                ws.Cell(row, 4).Value = line.AccountCategory;
                SetNumberCell(ws, row, 5, line.TotalDebits);
                SetNumberCell(ws, row, 6, line.TotalCredits);
                SetNumberCell(ws, row, 7, line.Balance);
                SetNumberCell(ws, row, 8, line.TranslatedDebits);
                SetNumberCell(ws, row, 9, line.TranslatedCredits);
                SetNumberCell(ws, row, 10, line.TranslatedBalance);
                ws.Cell(row, 11).Value = (double)line.ExchangeRateApplied;
                ws.Cell(row, 11).Style.NumberFormat.Format = "0.000000";
                row++;
            }

            // Section total
            for (var col = 1; col <= headers.Length; col++)
            {
                var cell = ws.Cell(row, col);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EFF6FF");
            }

            ws.Cell(row, 1).Value = $"Total {section.SectionName}";
            SetNumberCell(ws, row, 5, section.SectionTotalDebits);
            SetNumberCell(ws, row, 6, section.SectionTotalCredits);
            SetNumberCell(ws, row, 7, section.SectionBalance);
            SetNumberCell(ws, row, 8, section.TranslatedSectionTotalDebits);
            SetNumberCell(ws, row, 9, section.TranslatedSectionTotalCredits);
            SetNumberCell(ws, row, 10, section.TranslatedSectionBalance);
            row += 2;
        }

        // Summary block
        WriteExcelCell(ws, row++, 1, "Summary", bold: true, fontSize: 12);
        row = WriteExcelSummary(ws, row, report, currency);

        // Auto-fit columns
        ws.Columns().AdjustToContents(1, row);
        ws.Column(2).Width = 35;

        // ── Exchange rates sheet ──────────────────────────────────────────────
        if (report.ExchangeRatesUsed.Count > 0)
        {
            var rateWs = workbook.Worksheets.Add("Exchange Rates");
            WriteExcelCell(rateWs, 1, 1, "From", bold: true, bgColor: XLColor.FromHtml("#1E40AF"), fontColor: XLColor.White);
            WriteExcelCell(rateWs, 1, 2, "To", bold: true, bgColor: XLColor.FromHtml("#1E40AF"), fontColor: XLColor.White);
            WriteExcelCell(rateWs, 1, 3, "Rate", bold: true, bgColor: XLColor.FromHtml("#1E40AF"), fontColor: XLColor.White);
            WriteExcelCell(rateWs, 1, 4, "Type", bold: true, bgColor: XLColor.FromHtml("#1E40AF"), fontColor: XLColor.White);
            WriteExcelCell(rateWs, 1, 5, "As Of Date", bold: true, bgColor: XLColor.FromHtml("#1E40AF"), fontColor: XLColor.White);

            var rateRow = 2;
            foreach (var rate in report.ExchangeRatesUsed)
            {
                rateWs.Cell(rateRow, 1).Value = rate.FromCurrency;
                rateWs.Cell(rateRow, 2).Value = rate.ToCurrency;
                rateWs.Cell(rateRow, 3).Value = (double)rate.RateUsed;
                rateWs.Cell(rateRow, 3).Style.NumberFormat.Format = "0.000000";
                rateWs.Cell(rateRow, 4).Value = rate.TranslationType.ToString();
                rateWs.Cell(rateRow, 5).Value = rate.RateAsOfDate?.ToString("yyyy-MM-dd") ?? "Period Average";
                rateRow++;
            }

            rateWs.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        var fileName = BuildFileName(report, "xlsx");
        return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static void WriteExcelCell(
        IXLWorksheet ws, int row, int col, string value,
        bool bold = false, int? fontSize = null,
        XLColor? bgColor = null, XLColor? fontColor = null)
    {
        var cell = ws.Cell(row, col);
        cell.Value = value;
        if (bold) cell.Style.Font.Bold = true;
        if (fontSize.HasValue) cell.Style.Font.FontSize = fontSize.Value;
        if (bgColor is not null) cell.Style.Fill.BackgroundColor = bgColor;
        if (fontColor is not null) cell.Style.Font.FontColor = fontColor;
    }

    private static void SetNumberCell(IXLWorksheet ws, int row, int col, decimal? value)
    {
        var cell = ws.Cell(row, col);
        cell.Value = value.HasValue ? (double)value.Value : 0d;
        cell.Style.NumberFormat.Format = "#,##0.00";
    }

    private static int WriteExcelSummary(IXLWorksheet ws, int startRow, FinancialReportDto report, string currency)
    {
        var row = startRow;

        void AddRow(string label, decimal? value, bool highlight = false)
        {
            ws.Cell(row, 1).Value = label;
            SetNumberCell(ws, row, 2, value);
            if (highlight)
            {
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(value >= 0 ? "#D1FAE5" : "#FEE2E2");
            }
            row++;
        }

        if (report.NetIncome.HasValue)
        {
            AddRow($"Total Revenue ({currency})", report.TotalRevenue);
            AddRow($"Total Expenses ({currency})", report.TotalExpenses);
            AddRow($"Net Income ({currency})", report.NetIncome, highlight: true);
        }

        if (report.TotalAssets.HasValue)
        {
            AddRow($"Total Assets ({currency})", report.TotalAssets);
            AddRow($"Total Liabilities ({currency})", report.TotalLiabilities);
            AddRow($"Total Equity ({currency})", report.TotalEquity);
            AddRow($"Liabilities + Equity ({currency})", report.TotalLiabilitiesAndEquity);
            ws.Cell(row, 1).Value = "Balanced?";
            ws.Cell(row, 2).Value = report.IsBalanced == true ? "✔ Yes" : "✘ No";
            ws.Cell(row, 2).Style.Font.FontColor = report.IsBalanced == true
                ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");
            row++;
        }

        if (report.TrialBalanceTotalDebits.HasValue)
        {
            AddRow($"Total Debits ({currency})", report.TrialBalanceTotalDebits);
            AddRow($"Total Credits ({currency})", report.TrialBalanceTotalCredits);
        }

        return row;
    }

    // ── PDF ───────────────────────────────────────────────────────────────────

    private static (byte[] Data, string ContentType, string FileName) ExportToPdf(FinancialReportDto report)
    {
        var currency = report.ReportingCurrencyCode;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));

                // ── Header ────────────────────────────────────────────────────
                page.Header()
                    .BorderBottom(1).BorderColor(Colors.Blue.Darken1)
                    .PaddingBottom(6)
                    .Column(col =>
                    {
                        col.Item().Text(report.ReportTitle)
                            .FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Period: {report.FromDate:dd MMM yyyy} – {report.ToDate:dd MMM yyyy}");
                            row.RelativeItem().Text($"Currency: {currency}  |  Translation: {report.TranslationType}");
                            row.RelativeItem().AlignRight().Text($"Generated: {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
                        });

                        col.Item().Text($"Entities: {string.Join(", ", report.BusinessNames)}")
                            .Italic().FontColor(Colors.Grey.Darken1);
                    });

                // ── Content ───────────────────────────────────────────────────
                page.Content()
                    .PaddingVertical(0.5f, Unit.Centimetre)
                    .Column(mainCol =>
                    {
                        foreach (var section in report.Sections)
                        {
                            // Section header
                            mainCol.Item()
                                .PaddingTop(8)
                                .Background(Colors.Blue.Lighten4)
                                .Padding(4)
                                .Text(section.SectionName.ToUpperInvariant())
                                .SemiBold().FontSize(9).FontColor(Colors.Blue.Darken2);

                            // Account lines table
                            mainCol.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.ConstantColumn(60);  // Account #
                                    cols.RelativeColumn(3);   // Name
                                    cols.RelativeColumn(1.5f);// Category
                                    cols.RelativeColumn(1.5f);// Debits (base)
                                    cols.RelativeColumn(1.5f);// Credits (base)
                                    cols.RelativeColumn(1.5f);// Balance (base)
                                    cols.RelativeColumn(1.5f);// Debits (translated)
                                    cols.RelativeColumn(1.5f);// Credits (translated)
                                    cols.RelativeColumn(1.5f);// Balance (translated)
                                });

                                table.Header(header =>
                                {
                                    static IContainer PdfHeaderCell(IContainer c)
                                        => c.Background(Colors.Blue.Darken1)
                                             .Padding(3)
                                             .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold().FontSize(7.5f));

                                    header.Cell().Element(PdfHeaderCell).Text("Acct #");
                                    header.Cell().Element(PdfHeaderCell).Text("Account Name");
                                    header.Cell().Element(PdfHeaderCell).Text("Category");
                                    header.Cell().Element(PdfHeaderCell).AlignRight().Text("Debits");
                                    header.Cell().Element(PdfHeaderCell).AlignRight().Text("Credits");
                                    header.Cell().Element(PdfHeaderCell).AlignRight().Text("Balance");
                                    header.Cell().Element(PdfHeaderCell).AlignRight().Text($"Debits ({currency})");
                                    header.Cell().Element(PdfHeaderCell).AlignRight().Text($"Credits ({currency})");
                                    header.Cell().Element(PdfHeaderCell).AlignRight().Text($"Balance ({currency})");
                                });

                                var altRow = false;
                                foreach (var line in section.Lines)
                                {
                                    var bg = altRow ? Colors.Grey.Lighten4 : Colors.White;
                                    altRow = !altRow;

                                    IContainer DataCell(IContainer c) => c.Background(bg).Padding(3);

                                    table.Cell().Element(DataCell).Text(line.AccountNumber);
                                    table.Cell().Element(DataCell).Text(line.AccountName);
                                    table.Cell().Element(DataCell).Text(line.AccountCategory);
                                    table.Cell().Element(DataCell).AlignRight().Text(FormatDecimal(line.TotalDebits));
                                    table.Cell().Element(DataCell).AlignRight().Text(FormatDecimal(line.TotalCredits));
                                    table.Cell().Element(DataCell).AlignRight().Text(FormatDecimal(line.Balance));
                                    table.Cell().Element(DataCell).AlignRight().Text(FormatDecimal(line.TranslatedDebits));
                                    table.Cell().Element(DataCell).AlignRight().Text(FormatDecimal(line.TranslatedCredits));
                                    table.Cell().Element(DataCell).AlignRight()
                                        .Text(FormatDecimal(line.TranslatedBalance))
                                        .FontColor(line.TranslatedBalance < 0 ? Colors.Red.Darken2 : Colors.Black);
                                }

                                // Section total row
                                IContainer TotalCell(IContainer c)
                                    => c.Background(Colors.Blue.Lighten3).Padding(3)
                                         .DefaultTextStyle(x => x.SemiBold());

                                table.Cell().Element(TotalCell).Text("");
                                table.Cell().Element(TotalCell).Text($"Total {section.SectionName}");
                                table.Cell().Element(TotalCell).Text("");
                                table.Cell().Element(TotalCell).AlignRight().Text(FormatDecimal(section.SectionTotalDebits));
                                table.Cell().Element(TotalCell).AlignRight().Text(FormatDecimal(section.SectionTotalCredits));
                                table.Cell().Element(TotalCell).AlignRight().Text(FormatDecimal(section.SectionBalance));
                                table.Cell().Element(TotalCell).AlignRight().Text(FormatDecimal(section.TranslatedSectionTotalDebits));
                                table.Cell().Element(TotalCell).AlignRight().Text(FormatDecimal(section.TranslatedSectionTotalCredits));
                                table.Cell().Element(TotalCell).AlignRight().Text(FormatDecimal(section.TranslatedSectionBalance));
                            });
                        }

                        // Summary block
                        mainCol.Item().PaddingTop(16).Column(sumCol =>
                        {
                            sumCol.Item().Text("Summary").SemiBold().FontSize(10).FontColor(Colors.Blue.Darken2);
                            sumCol.Item().PaddingTop(4).Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(3);
                                    cols.RelativeColumn(2);
                                });

                                void SumRow(string label, string value, bool highlight = false)
                                {
                                    var bgColor = highlight ? Colors.Green.Lighten4 : Colors.White;
                                    if (highlight)
                                    {
                                        table.Cell().Background(bgColor).Padding(3).Text(label).SemiBold();
                                        table.Cell().Background(bgColor).Padding(3).AlignRight().Text(value).SemiBold();
                                    }
                                    else
                                    {
                                        table.Cell().Background(bgColor).Padding(3).Text(label);
                                        table.Cell().Background(bgColor).Padding(3).AlignRight().Text(value);
                                    }
                                }

                                if (report.NetIncome.HasValue)
                                {
                                    SumRow($"Total Revenue ({currency})", FormatDecimal(report.TotalRevenue));
                                    SumRow($"Total Expenses ({currency})", FormatDecimal(report.TotalExpenses));
                                    SumRow($"Net Income ({currency})", FormatDecimal(report.NetIncome), highlight: true);
                                }

                                if (report.TotalAssets.HasValue)
                                {
                                    SumRow($"Total Assets ({currency})", FormatDecimal(report.TotalAssets));
                                    SumRow($"Total Liabilities ({currency})", FormatDecimal(report.TotalLiabilities));
                                    SumRow($"Total Equity ({currency})", FormatDecimal(report.TotalEquity));
                                    SumRow($"Liabilities + Equity ({currency})", FormatDecimal(report.TotalLiabilitiesAndEquity));
                                    SumRow("Balanced?", report.IsBalanced == true ? "Yes ✔" : "No ✘", highlight: true);
                                }

                                if (report.TrialBalanceTotalDebits.HasValue)
                                {
                                    SumRow($"Total Debits ({currency})", FormatDecimal(report.TrialBalanceTotalDebits));
                                    SumRow($"Total Credits ({currency})", FormatDecimal(report.TrialBalanceTotalCredits));
                                }
                            });
                        });

                        // Exchange rates used
                        if (report.ExchangeRatesUsed.Count > 0)
                        {
                            mainCol.Item().PaddingTop(12).Column(rateCol =>
                            {
                                rateCol.Item().Text("Exchange Rates Applied")
                                    .SemiBold().FontSize(9).FontColor(Colors.Grey.Darken2);

                                rateCol.Item().PaddingTop(4).Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn();
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        IContainer H(IContainer c) => c.Background(Colors.Grey.Lighten1).Padding(3);
                                        header.Cell().Element(H).Text("From").SemiBold();
                                        header.Cell().Element(H).Text("To").SemiBold();
                                        header.Cell().Element(H).Text("Rate").SemiBold();
                                        header.Cell().Element(H).Text("Type").SemiBold();
                                        header.Cell().Element(H).Text("As Of Date").SemiBold();
                                    });

                                    foreach (var rate in report.ExchangeRatesUsed)
                                    {
                                        table.Cell().Padding(3).Text(rate.FromCurrency);
                                        table.Cell().Padding(3).Text(rate.ToCurrency);
                                        table.Cell().Padding(3).Text(FormatDecimal(rate.RateUsed, 6));
                                        table.Cell().Padding(3).Text(rate.TranslationType.ToString());
                                        table.Cell().Padding(3).Text(rate.RateAsOfDate?.ToString("yyyy-MM-dd") ?? "Period Average");
                                    }
                                });
                            });
                        }
                    });

                // ── Footer ────────────────────────────────────────────────────
                page.Footer()
                    .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                    .PaddingTop(4)
                    .Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Monolithic Finance  |  Multi-Currency Report").FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber().FontColor(Colors.Grey.Medium);
                            text.Span(" of ").FontColor(Colors.Grey.Medium);
                            text.TotalPages().FontColor(Colors.Grey.Medium);
                        });
                    });
            });
        });

        var bytes = document.GeneratePdf();
        var fileName = BuildFileName(report, "pdf");
        return (bytes, "application/pdf", fileName);
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static string FormatDecimal(decimal? value, int decimals = 2)
        => value.HasValue
            ? value.Value.ToString($"N{decimals}", CultureInfo.InvariantCulture)
            : "0.00";

    private static string BuildFileName(FinancialReportDto report, string ext)
    {
        var slug = report.ReportType switch
        {
            FinancialReportType.ProfitAndLoss => "PnL",
            FinancialReportType.BalanceSheet => "BalanceSheet",
            FinancialReportType.TrialBalance => "TrialBalance",
            _ => "Report"
        };

        return $"{slug}_{report.ReportingCurrencyCode}_{report.ToDate:yyyyMMdd}.{ext}";
    }

    private static string TruncateSheetName(string name)
        => name.Length > 31 ? name[..31] : name;
}
