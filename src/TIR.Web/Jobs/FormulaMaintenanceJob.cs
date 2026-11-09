using Hangfire;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Jobs;

public sealed class FormulaMaintenanceJob(IFormulaService formulas, ILogger<FormulaMaintenanceJob> logger)
{
    /// <summary>
    /// عدم اجرای همزمان جاب برای جلوگیری از پردازش تکراری مجموعه یکسان.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [AutomaticRetry(Attempts = 1)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var utcOffsetMinutes = IranDateConverter.TehranOffsetMinutes(DateTime.UtcNow);
        const int maxRows = 10000;

        for (var part = 0; part < 10000; part++)
        {
            var marked = await formulas.MarkExpiredAsync(maxRows, cancellationToken);

            if (marked < maxRows)
                break;
        }

        var order = await formulas.GetCalculationOrderAsync(cancellationToken);

        foreach (var formula in order)
        {
            const int pageSize = 2000;

            for (var part = 0; part < 10000; part++)
            {
                int count;
                try
                {
                    count = await formulas.CalculateSetAsync(
                        new FormulaCalculateSetRequest(formula.FormulaId, utcOffsetMinutes, pageSize),
                        cancellationToken);
                }
                catch (DatabaseFailureException error) when (error.ErrorNumber == 52604)
                {
                    logger.LogWarning("فرمول {FormulaId} به دلیل نوع اجرا کننده از محاسبه مجموعه ای کنار گذاشته شد.",
                        formula.FormulaId);

                    break;
                }

                if (count < pageSize)
                    break;

                if (part == 9999)
                    logger.LogWarning("تعداد بخش های پردازش فرمول {FormulaId} از حد مجاز گذشت.", formula.FormulaId);
            }
        }
    }
}
