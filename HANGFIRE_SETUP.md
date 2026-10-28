# راه اندازی Hangfire در محیط توسعه

این راهنما جایگزین بخش قدیمی Hangfire در README است.

علت ساخته نشدن جدول ها در نسخه قبلی دو تنظیم بود:

1. مقدار `Hangfire:Enabled` برابر `false` بود و سرور Hangfire اصلا ثبت نمی شد.
2. مقدار `PrepareSchemaIfNecessary` در کد برابر `false` بود و اجازه نصب خودکار جدول ها را نمی داد.

در نسخه اصلاح شده، فایل `appsettings.Development.json` شامل مقادیر زیر است:

```json
"Hangfire": {
  "Enabled": true,
  "AutoCreateSchema": true
}
```

رشته اتصال Hangfire به همان دیتابیس `Test` اشاره می کند. در کد، `SchemaName = "dbo"` تنظیم شده است تا جدول های Hangfire نیز در اسکیمای `dbo` ایجاد شوند.

## اجرا

1. پروژه را در محیط `Development` اجرا کنید. پروفایل موجود در `launchSettings.json` این محیط را تنظیم کرده است.
2. کاربر SQL باید برای نصب اولیه در دیتابیس `Test` مجوز ایجاد جدول و ایندکس داشته باشد.
3. پس از اجرای موفق برنامه، جدول های Hangfire را بررسی کنید.

```sql
USE [Test];

SELECT [name]
FROM sys.tables
WHERE schema_id = SCHEMA_ID(N'dbo')
  AND [name] IN (N'Schema', N'Job', N'State', N'JobQueue', N'Server')
ORDER BY [name];
```

اگر جدول های داخلی ایجاد نشدند، ابتدا خطای ثبت شده در Console را بررسی کنید. رایج ترین دلیل پس از اصلاح تنظیمات، نداشتن مجوز ساخت جدول برای حساب SQL است.

پس از نصب اولیه می توانید `AutoCreateSchema` را `false` کنید. در این حالت Hangfire برای هر بار اجرا به تغییر ساختار پایگاه داده نیاز ندارد.

هنوز Job زمان بندی شده در پروژه تعریف نشده است. داشبورد نیز برای محیط Production فعال نشده است.

## نمایش Dashboard

داشبورد تنها در محیط `Development` و با فعال بودن Hangfire در مسیر `/hangfire` ارائه می شود. دسترسی پیش فرض Hangfire فقط درخواست های محلی را مجاز می داند.

با پروفایل فعلی پروژه، یکی از آدرس های زیر را در مرورگر همان سیستم باز کنید:

- `https://localhost:49982/hangfire`
- `http://localhost:49983/hangfire`

اگر سرور روی پورت دیگری اجرا شد، همان پورتی را که برنامه در Console نمایش می دهد قرار دهید.

Dashboard در Production فعال نیست. برای دسترسی غیر محلی باید مکانیزم Authentication و Authorization تایید شده اضافه شود؛ هیچ مجوز عمومی برای Dashboard تنظیم نشده است.

جدول `dbo.Schema` مخصوص Hangfire است و با `dbo.SchemaVersion` متعلق به پروژه TIR تفاوت دارد. ساختار داخلی Hangfire را با RowVersion مخصوص جداول تجاری TIR تغییر ندهید.
