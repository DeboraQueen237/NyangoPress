using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using NyangoPress.Application.Common;
using NyangoPress.Application.Customers;
using NyangoPress.Shared.Customers;

namespace NyangoPress.Infrastructure.Customers;

public sealed class CustomerImportExportService(
    ICustomerStore store,
    ICustomerService customerService) : ICustomerImportExportService
{
    public async Task<byte[]> ExportCsvAsync(CancellationToken ct = default)
    {
        var customers = await store.GetAllForExportAsync(ct);
        await using var ms = new MemoryStream();
        await using var csv = CsvWriter.Create(ms, new CsvWriterOptions(), leaveOpen: true);

        csv.WriteField("CustomerCode");
        csv.WriteField("CustomerType");
        csv.WriteField("FullName");
        csv.WriteField("Phone");
        csv.WriteField("WhatsAppNumber");
        csv.WriteField("Email");
        csv.WriteField("Address");
        csv.WriteField("PreferredLanguage");
        csv.WriteField("AllowWhatsApp");
        csv.WriteField("AllowSms");
        csv.WriteField("AllowEmail");
        await csv.EndRecordAsync(ct);

        foreach (var c in customers)
        {
            csv.WriteField(c.CustomerCode);
            csv.WriteField(c.CustomerType.ToString());
            csv.WriteField(c.FullName);
            csv.WriteField(c.Phone);
            csv.WriteField(c.WhatsAppNumber ?? string.Empty);
            csv.WriteField(c.Email ?? string.Empty);
            csv.WriteField(c.Address ?? string.Empty);
            csv.WriteField(c.ContactPreference.PreferredLanguage.ToString());
            csv.WriteField(c.ContactPreference.AllowWhatsApp);
            csv.WriteField(c.ContactPreference.AllowSms);
            csv.WriteField(c.ContactPreference.AllowEmail);
            await csv.EndRecordAsync(ct);
        }

        await csv.FlushAsync(ct);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportExcelAsync(CancellationToken ct = default)
    {
        var customers = await store.GetAllForExportAsync(ct);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Clients");

        var headers = new[] { "Code", "Type", "Nom complet", "Téléphone", "WhatsApp", "E-mail", "Adresse", "Langue", "Points" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B3C5D");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var row = 2;
        foreach (var c in customers)
        {
            ws.Cell(row, 1).Value = c.CustomerCode;
            ws.Cell(row, 2).Value = c.CustomerType.ToString();
            ws.Cell(row, 3).Value = c.FullName;
            ws.Cell(row, 4).Value = c.Phone;
            ws.Cell(row, 5).Value = c.WhatsAppNumber ?? string.Empty;
            ws.Cell(row, 6).Value = c.Email ?? string.Empty;
            ws.Cell(row, 7).Value = c.Address ?? string.Empty;
            ws.Cell(row, 8).Value = c.ContactPreference.PreferredLanguage.ToString();
            ws.Cell(row, 9).Value = c.LoyaltyPoints;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public Task<byte[]> GetCsvTemplateAsync(CancellationToken ct = default)
    {
        const string template = "CustomerType,FullName,Phone,WhatsAppNumber,Email,Address,PreferredLanguage\nIndividual,Marie-Claire Ngo,+237690112233,+237690112233,marie@exemple.cm,Bonapriso Douala,Fr\nCorporate,Hôtel La Falaise,+237677445566,,contact@falaise.cm,Bonanjo Douala,Fr\n";
        return Task.FromResult(Encoding.UTF8.GetBytes(template));
    }

    public Task<byte[]> GetExcelTemplateAsync(CancellationToken ct = default)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ModeleClients");
        var headers = new[] { "CustomerType", "FullName", "Phone", "WhatsAppNumber", "Email", "Address", "PreferredLanguage" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
        ws.Cell(2, 1).Value = "Individual";
        ws.Cell(2, 2).Value = "Jean Dupont";
        ws.Cell(2, 3).Value = "+237699001122";
        ws.Cell(2, 4).Value = "+237699001122";
        ws.Cell(2, 5).Value = "jean@exemple.cm";
        ws.Cell(2, 6).Value = "Akwa, Douala";
        ws.Cell(2, 7).Value = "Fr";
        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return Task.FromResult(ms.ToArray());
    }

    public async Task<Result<CustomerImportResultDto>> ImportAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        List<(int Row, string Type, string Name, string Phone, string? WhatsApp, string? Email, string? Address, string Lang)> rows;

        try
        {
            rows = ext switch
            {
                ".csv" => await ParseCsvAsync(fileStream, ct),
                ".xlsx" => ParseExcel(fileStream),
                _ => throw new InvalidOperationException("Format non supporté. Utilisez .csv ou .xlsx.")
            };
        }
        catch (Exception ex)
        {
            return Result.Failure<CustomerImportResultDto>("Import.InvalidFile", ex.Message);
        }

        var errors = new List<CustomerImportErrorDto>();
        var imported = 0;

        foreach (var r in rows)
        {
            var type = Enum.TryParse<CustomerType>(r.Type, true, out var parsedType)
                ? parsedType
                : CustomerType.Individual;
            var lang = Enum.TryParse<PreferredLanguage>(r.Lang, true, out var parsedLang)
                ? parsedLang
                : PreferredLanguage.Fr;

            var createResult = await customerService.CreateAsync(
                new CreateCustomerRequest(
                    type,
                    r.Name,
                    r.Phone,
                    r.WhatsApp,
                    r.Email,
                    r.Address,
                    null,
                    lang,
                    IgnoreDuplicateWarning: false),
                ct);

            if (createResult.IsSuccess)
            {
                imported++;
            }
            else
            {
                errors.Add(new CustomerImportErrorDto(r.Row, r.Name, createResult.Error.Code, createResult.Error.Message));
            }
        }

        return Result.Success(new CustomerImportResultDto(rows.Count, imported, errors.Count, errors));
    }

    private static async Task<List<(int, string, string, string, string?, string?, string?, string)>> ParseCsvAsync(Stream stream, CancellationToken ct)
    {
        using var csv = CsvReader.Create(stream, new CsvReaderOptions(), leaveOpen: true);

        var list = new List<(int, string, string, string, string?, string?, string?, string)>();
        if (!await csv.ReadAsync(ct))
            return list;

        csv.ReadHeader();

        var rowNum = 1;
        while (await csv.ReadAsync(ct))
        {
            rowNum++;
            list.Add((
                rowNum,
                GetOptionalField(csv, "CustomerType") ?? "Individual",
                GetOptionalField(csv, "FullName") ?? string.Empty,
                GetOptionalField(csv, "Phone") ?? string.Empty,
                GetOptionalField(csv, "WhatsAppNumber"),
                GetOptionalField(csv, "Email"),
                GetOptionalField(csv, "Address"),
                GetOptionalField(csv, "PreferredLanguage") ?? "Fr"));
        }
        return list;
    }

    private static string? GetOptionalField(CsvReader csv, string name)
        => csv.TryGetFieldIndex(name, out var idx) ? csv.GetStringOrNull(idx) : null;

    private static List<(int, string, string, string, string?, string?, string?, string)> ParseExcel(Stream stream)
    {
        using var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        var list = new List<(int, string, string, string, string?, string?, string?, string)>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= lastRow; r++)
        {
            var name = ws.Cell(r, 2).GetString();
            var phone = ws.Cell(r, 3).GetString();
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(phone)) continue;

            list.Add((
                r,
                ws.Cell(r, 1).GetString(),
                name,
                phone,
                ws.Cell(r, 4).GetString(),
                ws.Cell(r, 5).GetString(),
                ws.Cell(r, 6).GetString(),
                ws.Cell(r, 7).GetString()));
        }

        return list;
    }
}
