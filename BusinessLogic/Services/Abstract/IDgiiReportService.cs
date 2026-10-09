namespace Onion.BussinesLogic.Services.Abstract;

public interface IDgiiReportService
{
    Task<DgiiReportResult> Get607Async(int companyId, int year, int month, CancellationToken cancellationToken = default);
    Task<DgiiReportResult> Get606Async(int companyId, int year, int month, CancellationToken cancellationToken = default);
    Task<DgiiReportResult> Get608Async(int companyId, int year, int month, CancellationToken cancellationToken = default);
}

public sealed record DgiiReportResult(string Format, int Year, int Month, IReadOnlyList<string[]> Rows)
{
    public string ToPipeDelimited() => string.Join(Environment.NewLine, Rows.Select(row => string.Join("|", row.Select(Escape))));
    private static string Escape(string value) => value.Replace("|", " ").Replace("\r", " ").Replace("\n", " ");
}
