using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Http.Features;
using Serilog;
using TIR.Application;
using TIR.Application.Common;
using TIR.Infrastructure;
using TIR.Web.Jobs;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, _, logging) => logging
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("Test") ?? string.Empty);
    builder.Services.AddRazorPages();
    builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 256L * 1024 * 1024);
    builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 256L * 1024 * 1024);

    if (builder.Configuration.GetValue<bool>("Hangfire:Enabled"))
    {
        var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(hangfireConnection))
            throw new InvalidOperationException("رشته اتصال Hangfire تنظیم نشده است.");

        var createSchema = builder.Configuration.GetValue<bool>("Hangfire:AutoCreateSchema");
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
            {
                SchemaName = "dbo",
                PrepareSchemaIfNecessary = createSchema
            }));

        builder.Services.AddHangfireServer();
        builder.Services.AddScoped<FormulaMaintenanceJob>();
        builder.Services.AddScoped<BulkCleanupJob>();
    }

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // داشبورد بدون احراز هویت فقط در محیط توسعه و برای درخواست محلی فعال است.
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Hangfire:Enabled"))
        app.UseHangfireDashboard("/hangfire");

    // در صورت فعال بودن Hangfire، زمان بندی Jobها به صورت پیش فرض فعال است.
    // برای غیرفعال کردن آن مقدار Hangfire:Jobs:Enabled را false قرار دهید.
    var jobsEnabled = app.Configuration.GetValue<bool?>("Hangfire:Jobs:Enabled") ?? true;
    if (app.Configuration.GetValue<bool>("Hangfire:Enabled") && jobsEnabled)
    {
        using var scope = app.Services.CreateScope();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobs.AddOrUpdate<FormulaMaintenanceJob>(
            "tir-formula-midnight-iran",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(0, 0),
            new RecurringJobOptions { TimeZone = IranDateConverter.TehranTimeZone });

        recurringJobs.AddOrUpdate<BulkCleanupJob>(
            "tir-bulk-cleanup-hourly",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly(),
            new RecurringJobOptions { TimeZone = IranDateConverter.TehranTimeZone });
    }

    app.MapRazorPages();
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "خطا هنگام راه اندازی برنامه TIR");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
