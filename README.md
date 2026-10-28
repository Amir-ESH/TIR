# فاز دوم TIR - مرحله اول Backend

این پوشه شامل ساختار اولیه Backend با .NET 8 و Clean Architecture است. فایل های فاز اول دیتابیس در پوشه `database` خارج از این پوشه محفوظ مانده اند.

## ساختار و وابستگی ها

```text
TIR.Web --------------> TIR.Application ------> TIR.Domain
   |                          ^
   +-----> TIR.Infrastructure-+
```

| پروژه | مسئولیت | وابستگی خارجی |
| --- | --- | --- |
| `TIR.Domain` | مدل های دامنه و قراردادهای مستقل از فناوری | هیچ پکیج خارجی ندارد |
| `TIR.Application` | DTO، اعتبارسنجی اولیه، سناریو و قرارداد مخزن | FluentValidation، Mapperly |
| `TIR.Infrastructure` | اجرای SP با Dapper و اتصال SQL Server | Dapper، SqlClient، Mapperly |
| `TIR.Web` | راه اندازی DI و زیرساخت میزبانی، فعلا بدون UI | Serilog، Hangfire |

نسخه پکیج ها در `Directory.Packages.props` مدیریت می شود. نسخه مورد استفاده همه پروژه ها `net8.0` است.

## کارهای آماده شده

1. Solution چهار پروژه ای با وابستگی درست لایه ها.
2. تنظیم Serilog برای ثبت خطاها و درخواست ها روی Console.
3. ثبت سرویس ها با Dependency Injection.
4. نمونه کاربرد واقعی FluentValidation برای درخواست خواندن کارمند.
5. اتصال Async به SQL Server با `Microsoft.Data.SqlClient`.
6. اجرای `dbo.SP_Employee_Get` با Dapper و `CancellationToken`.
7. تبدیل مدل SQL به دامنه و دامنه به DTO توسط Mapperly.
8. آماده سازی اختیاری Hangfire برای اجرای Jobهای آینده.

## قرارداد پایگاه داده

در این مرحله فقط مسیر زیر پیاده شده است:

```text
IEmployeeQueryService.GetAsync
  -> GetEmployeeQueryValidator
  -> IEmployeeReadRepository.GetByIdAsync
  -> dbo.SP_Employee_Get(@Id, @IncludeDeleted)
  -> EmployeeDbMapper
  -> EmployeeMapper
  -> EmployeeDetailsDto
```

کل منطق اعتبار نهایی داده، CRUD و تراکنش ها در SQL Server باقی می ماند. هیچ متن Query برای عملیات پایدارسازی در C# نوشته نشده است. هیچ Controller و API مستقلی اضافه نشده است.

## پیش نیاز و اجرا

- .NET 8 SDK
- SQL Server 2019 و دیتابیس `Test` با نسخه های `004` و `005`
- دسترسی اتصال به دیتابیس از سیستمی که Backend روی آن اجرا می شود

ابتدا رشته اتصال را از User Secrets یا متغیر محیطی وارد کنید؛ از ثبت رمز عبور در مخزن خودداری کنید:

```bash
cd backend
dotnet user-secrets init --project src/TIR.Web
dotnet user-secrets set 'ConnectionStrings:Test' 'Server=YOUR_SERVER;Database=Test;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;' --project src/TIR.Web
dotnet restore TIR.sln
dotnet build TIR.sln --no-restore
dotnet run --project src/TIR.Web
```

رشته اتصال بالا فقط نمونه است. برای محیط عملیاتی، اعتبار گواهی سرور را بررسی کنید و بدون بررسی امنیتی از `TrustServerCertificate=True` استفاده نکنید.

در این مرحله برنامه Host اجرا می شود اما صفحه و Endpoint ندارد؛ دریافت پاسخ `404` در مرورگر طبیعی است. صفحات Razor در فاز سوم ایجاد می شوند. برای بررسی اتصال واقعی، در مرحله تست یکپارچه فراخوانی سرویس پیاده می شود.

## وضعیت Hangfire

در `appsettings.json` مقدار `Hangfire:Enabled` برابر `false` است. دلیل این تصمیم این است که Hangfire برای Jobها به جداول اختصاصی نیاز دارد و نباید هنگام اجرای اولیه Backend به صورت ناخواسته ساختار دیتابیس `Test` را تغییر دهد.

برای فعال سازی بعدی:

1. محل نگهداری اطلاعات Hangfire و رشته اتصال `ConnectionStrings:Hangfire` تعیین شود.
2. ساختار SQL مخصوص Hangfire با فرآیند نصب کنترل شده ساخته شود.
3. سپس مقدار `Hangfire:Enabled` برابر `true` شود.
4. Jobهای انقضای فرمول و پاکسازی Batch بعد از تایید قرارداد زمان بندی اضافه شوند.

گزینه `PrepareSchemaIfNecessary=false` از ایجاد خودکار جداول Hangfire جلوگیری می کند. Dashboard نیز فعال نشده است تا بدون Authentication در دسترس قرار نگیرد.

## کارهای مرحله های بعد Backend

- دریافت فهرست کارمندان و مقدارهای داینامیک، CRUD و مدیریت RowVersion.
- ثبت داده های نوع دار و مدیریت خطاهای کدهای `THROW` در SQL.
- مدیریت AttributeDefinition، گزینه ها و فرمول ها.
- اجرای Bulk با Staging و `SqlBulkCopy`، دریافت خطاهای هر کارمند.
- جستجوی پیشرفته، صفحه بندی و تبدیل شمسی و UTC برای نمایش.
- فعال سازی امن Hangfire و تعریف Jobهای مورد نیاز.
- تست واقعی اتصال، خطا، کنترل همزمانی و پردازش گروهی همراه با دیتابیس.

## محدودیت تایید

محیط تولید این فایل ها دارای `dotnet` نیست؛ بنابراین Build، بازیابی پکیج ها و اجرای واقعی SQL Server در این محیط انجام نشده است. تا زمان اجرای `dotnet build` و تست اتصال روی محیط شما، صحیح بودن کامپایل و یکپارچگی اجرایی قطعی نیست.

**ریسک نگهداری:** بر اساس سیاست مایکروسافت، پشتیبانی .NET 8 در ۱۰ نوامبر ۲۰۲۶ پایان می یابد. مطابق درخواست فعلی پروژه با .NET 8 نوشته شده است، ولی پیش از انتشار عملیاتی باید برای ارتقا به یک نسخه تحت پشتیبانی تصمیم گرفته شود.
