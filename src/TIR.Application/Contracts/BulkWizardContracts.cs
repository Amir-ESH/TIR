namespace TIR.Application.Contracts;

public sealed record BulkWizardEntry(int InputOrdinal, int? ExcelRow, long? EmployeeId,
    byte[]? ExpectedEmployeeRowVersion, string? EmployeeCode, string? FirstName,
    string? LastName, IReadOnlyList<BulkValueInput> Values);

public sealed class BulkWizardStatusDto
{
    public Guid Id { get; set; }
    
    public byte OperationKind { get; set; }
    
    public long? FormulaId { get; set; }
    
    public byte Status { get; set; }
    
    public int TotalCount { get; set; }
    
    public int ProcessedCount { get; set; }
    
    public int SucceededCount { get; set; }
    
    public int FailedCount { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    
    public DateTime? FinishedAtUtc { get; set; }
    
    public string? FailureMessage { get; set; }
}

public sealed class BulkWizardErrorDto
{
    public int InputOrdinal { get; set; }
    
    public int? ExcelRow { get; set; }
    
    public long? EmployeeId { get; set; }
    
    public long? AttributeId { get; set; }
    
    public string ErrorCode { get; set; } = string.Empty;
    
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed record BulkWizardReport(BulkWizardStatusDto Header, IReadOnlyList<BulkWizardErrorDto> Errors);

public interface IBulkWizardRepository
{
    Task<Guid> StageAsync(byte operationKind, long? formulaId, IReadOnlyList<BulkWizardEntry> rows,
        CancellationToken cancellationToken);

    Task ProcessChunkAsync(Guid workId, int chunkNumber, int chunkSize, CancellationToken cancellationToken);
    
    Task<(Guid WorkId, int TotalCount)> StageFormulaAllAsync(long formulaId, CancellationToken cancellationToken);
    
    Task ProcessFormulaChunkAsync(Guid workId, int chunkNumber, int chunkSize,
        short utcOffsetMinutes, CancellationToken cancellationToken);
    
    Task<(byte OperationKind, int TotalCount)> QueueAsync(Guid workId, CancellationToken cancellationToken);
    
    Task<BulkWizardReport?> ReportAsync(Guid workId, int errorPage, CancellationToken cancellationToken);
    
    Task<int> CleanupAsync(DateTime olderThanUtc, CancellationToken cancellationToken);
}
