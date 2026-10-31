using Hangfire;
using TIR.Application.Contracts;

namespace TIR.Web.Jobs;

public sealed class BulkCleanupJob(IBulkService bulk, ILogger<BulkCleanupJob> logger)
{
    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 1)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // درخواست های پایان یافته پس از ۲۴ ساعت به صورت فیزیکی حذف می شوند.
        var olderThanUtc = DateTime.UtcNow.AddHours(-24);
        var total = 0;
        for (var part = 0; part < 1000; part++)
        {
            var count = await bulk.CleanupAsync(olderThanUtc, 1000, cancellationToken);
            total += count;
            if (count < 1000) break;
        }
        logger.LogInformation("تعداد درخواست های گروهی پاکسازی شده: {DeletedCount}", total);
    }
}
