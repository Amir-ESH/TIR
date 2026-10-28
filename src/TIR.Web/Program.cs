using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using TIR.Application;
using TIR.Infrastructure;

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

    // فعال کردن Hangfire و ایجاد جداول دو تنظیم مستقل هستند.
    if (builder.Configuration.GetValue<bool>("Hangfire:Enabled"))
    {
        var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(hangfireConnection))
            throw new InvalidOperationException("رشته اتصال Hangfire تنظیم نشده است.");

        // ساخت خودکار جداول فقط هنگام فعال بودن AutoCreateSchema مجاز است.
        var createSchema = builder.Configuration.GetValue<bool>("Hangfire:AutoCreateSchema");

        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
            {
                // جدول های داخلی Hangfire در اسکیمای dbo ساخته می شوند.
                SchemaName = "dbo",
                PrepareSchemaIfNecessary = createSchema
            }));
        builder.Services.AddHangfireServer();
    }

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    // داشبورد فقط در محیط توسعه فعال است و دسترسی پیش فرض آن محلی است.
    if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Hangfire:Enabled"))
        app.UseHangfireDashboard("/hangfire");

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
