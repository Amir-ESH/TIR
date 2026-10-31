# Backend پروژه TIR

پروژه بر پایه .NET 8، Clean Architecture، SQL Server، Dapper، Microsoft.Data.SqlClient، FluentValidation، Mapperly، Serilog و Hangfire آماده شده است.

مستندات:

- [BACKEND_IMPLEMENTATION.md](BACKEND_IMPLEMENTATION.md): قرارداد تمام ۳۳ Stored Procedure، سرویس ها، خطاها، RowVersion، Bulk، جستجو، تبدیل تاریخ، محدودیت ها و راهنمای اجرا.
- [HANGFIRE_SETUP.md](HANGFIRE_SETUP.md): راه اندازی Hangfire و مسیر داشبورد در محیط توسعه.

## ساختار پروژه

| پروژه | مسئولیت |
| --- | --- |
| `TIR.Domain` | مدل مستقل دامنه |
| `TIR.Application` | قراردادها، سناریوها، FluentValidation و تبدیل تاریخ |
| `TIR.Infrastructure` | اجرای Stored Procedure با Dapper و بارگذاری حجیم با SqlBulkCopy |
| `TIR.Web` | میزبان، Serilog، Hangfire و Jobها؛ بدون Razor Pages در فاز دوم |

## شروع

```bash
cd backend
dotnet restore TIR.sln
dotnet build TIR.sln --no-restore
dotnet run --project src/TIR.Web
```

رشته های اتصال باید در فایل تنظیمات محلی یا User Secrets تنظیم شوند. فایل `database/DataBase.sql` در پوشه مجاور Backend مبنای قراردادهای SQL است.

تمامی عملیات پایدارسازی از Stored Procedure عبور می کنند. فاز Razor Pages هنوز شروع نشده است. Build و تست یکپارچه دیتابیس باید روی محیط دارای .NET SDK و SQL Server انجام شوند.
