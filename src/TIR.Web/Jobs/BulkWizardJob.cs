using Hangfire;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Jobs;

// هر Job تنها یک بخش ده هزار تایی را پردازش می کند.
[AutomaticRetry(Attempts = 0)]
public sealed class BulkWizardJob(IBulkWizardRepository repository, ILogger<BulkWizardJob> logger)
{
    public async Task ProcessAsync(Guid workId, int chunkNumber, byte operationKind,
        CancellationToken cancellationToken)
    {
        try
        {
            if (operationKind == 3)
                await repository.ProcessFormulaChunkAsync(workId, chunkNumber, 10000,
                    IranDateConverter.TehranOffsetMinutes(DateTime.UtcNow), cancellationToken);
            else
                await repository.ProcessChunkAsync(workId, chunkNumber, 10000, cancellationToken);

            logger.LogInformation("بخش {Chunk} از عملیات گروهی {WorkId} انجام شد", chunkNumber + 1, workId);
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در بخش {Chunk} درخواست {WorkId}", chunkNumber + 1, workId);
            throw;
        }
    }
}
