# راهنمای پیاده سازی Backend پروژه TIR

این نسخه بر اساس `database/DataBase.sql` موجود در بسته پروژه آماده شده است. منطق ثبت کارمند در یک رویه `dbo.SP_Employee_Create` یکپارچه شده و رویه قدیمی `SP_Employee_CreateWithValues` حذف شده است. نسخه هدف `net8.0` است و UI در این فاز ایجاد نشده است.

## معماری

| لایه | مسئولیت |
| --- | --- |
| `TIR.Domain` | مدل مستقل کارمند |
| `TIR.Application` | قراردادهای درخواست و پاسخ، FluentValidation، سرویس های هماهنگ کننده، تبدیل تاریخ شمسی |
| `TIR.Infrastructure` | فراخوانی ۳۲ رویه با Dapper، SqlClient، انتقال حجیم با SqlBulkCopy و تبدیل خطاها |
| `TIR.Web` | تنظیمات Host، Serilog، Hangfire Server، Jobهای دوره ای و داشبورد توسعه |

قانون اصلی: **هیچ منطق CRUD، اعتبارسنجی تجاری، محاسبه فرمول یا مدیریت تراکنش تجاری در C# تکرار نشده است.**

## سرویس های آماده شده

- `IEmployeeQueryService`: خواندن جزئیات یک کارمند و `RowVersion`.
- `IEmployeeService`: ایجاد کارمند از یک مسیر واحد، با کد پرسنلی و مقادیر اولیه اختیاری، ویرایش، حذف منطقی، بازیابی، فهرست و جستجوی پیشرفته.
- `IAttributeService`: مدیریت تعریف مشخصه و گزینه های انتخاب تکی.
- `IAttributeValueService`: دریافت مقدار، فهرست فرم و ذخیره یا خالی کردن مقدار نوع دار.
- `IFormulaService`: ثبت فرمول، وابستگی، ترتیب، محاسبه فردی و مجموعه ای، نامعتبر کردن و تشخیص انقضا.
- `IBulkService`: بارگذاری گروهی، پردازش، گزارش، حذف پس از نمایش و پاکسازی دوره ای.

تمام این سرویس ها از طریق Dependency Injection در پروژه `TIR.Web` در دسترس هستند. فهرست رویه های مرتبط:

| گروه | Stored Procedures |
| --- | --- |
| مشخصه | `SP_AttributeDefinition_Create`, `SP_AttributeDefinition_Get`, `SP_AttributeDefinition_List`, `SP_AttributeDefinition_Update`, `SP_AttributeDefinition_SoftDelete`, `SP_AttributeDefinition_Restore` |
| گزینه | `SP_AttributeOption_Create`, `SP_AttributeOption_List` |
| کارمند | `SP_Employee_Create`, `SP_Employee_Get`, `SP_Employee_List`, `SP_Employee_Update`, `SP_Employee_SoftDelete`, `SP_Employee_Restore`, `SP_Employee_SearchAdvanced` |
| مقدار | `SP_EmployeeAttributeValue_Get`, `SP_EmployeeAttributeValue_List`, `SP_EmployeeAttributeValue_Save` |
| فرمول | `SP_AttributeFormula_Register`, `SP_FormulaDependency_Add`, `SP_Formula_GetCalculationOrder`, `SP_Formula_Calculate`, `SP_Formula_CalculateAll`, `SP_Formula_CalculateSet`, `SP_Formula_MarkExpired`, `SP_Formula_InvalidateAll`, `SP_Formula_InvalidateDependents` |
| گروهی | `SP_Batch_Create`, `SP_Batch_Process`, `SP_Batch_GetReport`, `SP_Batch_Delete`, `SP_Batch_Cleanup` |

### قرارداد جدید ایجاد کارمند

- تمام ثبت ها فقط از `dbo.SP_Employee_Create` انجام می شوند؛ `EmployeeRepository` دیگر مسیر جداگانه ای برای مشخصه های اولیه ندارد.
- `EmployeeCreateRequest.EmployeeCode` اختیاری است. کد خالی یا `null` باعث تولید کد برابر بزرگ ترین کد **کاملاً عددی** موجود + ۱ می شود؛ کدهای `EMP0001` در شمارش شرکت نمی کنند. کدهای مربوط به کارمندان حذف نرم شده نیز رزرو باقی می مانند.
- نام و نام خانوادگی همچنان اجباری هستند. `InitialValues` در صورت عدم ارسال به TVP خالی تبدیل می شود.
- اگر مشخصه ورودی اجباری و فعال تعریف شده باشد، باید مقدار درست آن در `InitialValues` ارسال شود، وگرنه کل ثبت در SQL رد می شود. اعتبارسنجی نوع و گزینه انتخابی نیز در همین تراکنش انجام می شود.
- تولید خودکار کد برای جلوگیری از تکرار همزمان از قفل انحصاری موقت روی جدول `Employee` استفاده می کند. در نرخ ثبت بسیار بالا، این قسمت باید با شمارنده مستقل و هماهنگ با ثبت کد دستی بهینه شود.
- برای اعمال تغییر در دیتابیس موجود، ابتدا پس از گرفتن Backup فایل `database/20261031_UnifyEmployeeCreate.sql` را اجرا کنید. اجرای مجدد اسکریپت کامل `DataBase.sql` روی دیتابیس موجود توصیه نمی شود.

## قرارداد نسخه و همزمانی

1. ابتدا کارمند، مشخصه یا مقدار را بخوانید و `RowVersion` هشت بایتی را دریافت کنید.
2. در ویرایش، حذف و بازیابی، نسخه خوانده شده را در `ExpectedRowVersion` بفرستید.
3. برای درج مقدار جدید، `ExpectedRowVersion = null` ارسال کنید. برای خالی کردن مقدار، `OperationKind = 2` و تمام ستون های مقدار را `null` قرار دهید.
4. `Employee.RowVersion` فقط اطلاعات ثابت کارمند را کنترل می کند. نسخه هر مقدار در جدول `EmployeeAttributeValue` مستقل است.
5. تعارض در SQL تشخیص داده می شود و در Backend به `DatabaseFailureException` با `Kind = Concurrency` تبدیل می شود.

خطاهای `52xxx` در SQL خطاهای تجاری هستند. خطای یکتا بودن `2601/2627` با `Kind = Duplicate` و Deadlock `1205` با `Kind = Transient` برگردانده می شود. فقط خواندن در صورت Deadlock حداکثر دو بار تلاش دوباره دارد. عملیات نوشتن به صورت خودکار تکرار نمی شوند.

## قرارداد مقادیر نوع دار

- متن: `StringValue`، عدد صحیح: `IntegerValue`، اعشار: `DecimalValue` با دقت `decimal(23,3)`.
- تاریخ: `DateValue`، زمان: `TimeValue` از نوع `TimeSpan`، زمان کامل UTC: `DateTimeUtc`.
- بولین: `BooleanValue` و انتخاب تکی: `OptionId`.
- `SourceKind` و `DataKind` فقط از متادیتای تعریف مشخصه دریافت می شوند؛ تشخیص نوع صحیح با SQL است.
- ورودی `DateValue` در C# به `DateTime.Date` تبدیل و به ستون SQL `date` فرستاده می شود.
- `IranDateConverter` برای تبدیل تاریخ شمسی، تاریخ میلادی و وقت ایران ایجاد شده است. مقدار `DateTimeUtc` باید از لایه ارائه به UTC تبدیل شود؛ نمونه: `IranDateConverter.TehranLocalToUtc(localDateTime)`.

## جستجوی پیشرفته

از `AdvancedEmployeeSearchRequest` با آرایه `SearchCondition` استفاده کنید. هر شرط دارای `Group`, `AttributeId`, `Operator`, `Value`, `To` است. گروه های متفاوت با OR و شروط یک گروه با AND به هم متصل می شوند. مقادیر باید به شکل متن سازگار با SQL باشند؛ تاریخ `yyyy-MM-dd` و تاریخ و زمان UTC در قالب ISO 8601. استفاده از `IranDateConverter.ParsePersianDate` برای ورودی شمسی در PageModel آینده لازم است.

Backend آرایه را با JSON serializer به پارامتر `FiltersJson` تبدیل می کند. هیچ مقدار کاربر در متن SQL ساخته شده در Backend الحاق نمی شود. قرارداد SQL حداکثر ۶۴ شرط و ۱۶ گروه است.

## عملیات Bulk

مسیر پیشنهادی:

1. `IBulkService.UploadAndProcessAsync(...)` یک `IAsyncEnumerable<BulkEmployeeInput>` دریافت می کند؛ برای ۵۰۰ هزار کارمند لازم نیست تمام ورودی در حافظه Backend باشد.
2. `SP_Batch_Create` در تراکنش بارگذاری فراخوانی می شود.
3. `SqlBulkCopy` با `BatchId` اختصاصی، اطلاعات را در جدول های `EmployeeInput` و `ValueInput` به صورت بخش های محدود می نویسد. تراکنش بارگذاری تا تکمیل انتقال باز می ماند.
4. `SP_Batch_Process` فقط یک بار فراخوانی می شود و SQL اعتبارسنجی و ثبت کارمندان معتبر را به صورت مجموعه ای انجام می دهد.
5. خروجی شامل آمار، نتیجه هر کارمند و تمام خطاهای مرتب شده است. UI بعد از نمایش گزارش می تواند `IBulkService.DeleteAsync(batchId)` را فراخوانی کند.
6. Job پاکسازی، گزارش های پایان یافته قدیمی تر از ۲۴ ساعت را به صورت دوره ای با `SP_Batch_Cleanup` حذف می کند.

تنظیم های `BulkRequestOptions` شامل `TransferBatchSize` و `EmployeesPerTransaction` هستند. پیش فرض به ترتیب ۳۰۰۰ و ۲۰۰۰ است. ارسال دوباره همان درخواست به عنوان درخواست تازه پذیرفته می شود. هر رکورد مقدار دارای نسخه هشت بایتی مستقل است.

**محدودیت موجود SQL:** `SP_Batch_Cleanup` فقط Batchهای با وضعیت پایانی ۳، ۴ و ۵ را پاک می کند. اگر انتقال انجام شود اما قبل از شروع `SP_Batch_Process` ارتباط قطع شود، Batch با وضعیت صفر باقی می ماند و این رویه آن را پاک نمی کند. اصلاح این مورد نیاز به تغییر جداگانه و تایید شده SP در دیتابیس دارد.

## Hangfire

وقتی `Hangfire:Enabled=true` باشد دو Job زیر ثبت می شوند. کلید `Hangfire:Jobs:Enabled` به صورت پیش فرض فعال است و با قرار دادن مقدار `false` می توانید ثبت Jobها را غیرفعال کنید:

- `tir-formula-midnight-iran`: هر روز ساعت ۰۰:۰۰ به وقت تهران نتایج منقضی را Stale می کند و فرمول های قابل محاسبه مجموعه ای را طبق ترتیب وابستگی محاسبه می کند.
- `tir-bulk-cleanup-hourly`: هر ساعت درخواست های پایان یافته قدیمی تر از ۲۴ ساعت را حذف می کند.

**محدودیت موجود SQL:** `SP_Formula_CalculateSet` فقط اجرا کننده های `IF` و `TF` را پشتیبانی می کند. فرمول های `FN` و `P` در Job شبانه با هشدار کنار گذاشته می شوند و هنوز برای محاسبه خودکار مجموعه ای نیاز به قرارداد SQL تکمیلی دارند. محاسبه دستی یک کارمند از مسیر `SP_Formula_CalculateAll` پشتیبانی می شود.

داشبورد فقط در محیط توسعه در `/hangfire` فعال است؛ در نسخه فعلی هیچ دسترسی ناشناس از شبکه به داشبورد داده نشده است.

## اجرای پروژه

```bash
cd backend
dotnet restore TIR.sln
dotnet build TIR.sln --no-restore
dotnet run --project src/TIR.Web
```

تنظیم های `ConnectionStrings:Test` و `ConnectionStrings:Hangfire` باید از User Secrets یا تنظیمات محلی تامین شوند. فایل `appsettings.Development.json` در نسخه ارسالی حاوی اطلاعات اتصال محیط شما است و نباید در مخزن عمومی منتشر شود.

پورت های پروفایل فعلی: `https://localhost:49982` و `http://localhost:49983`. چون Razor Pages در فاز سوم است، مسیر ریشه ممکن است ۴۰۴ بدهد؛ داشبورد Hangfire در `/hangfire` ارائه می شود.

## وضعیت راستی آزمایی و کارهای باقی مانده

- قرارداد ورودی و خروجی رویه ها با فایل `database/DataBase.sql` تطبیق داده شده است.
- Build واقعی و تست اتصال SQL در محیط تولید این بسته انجام نشده است، چون .NET SDK و دسترسی به سرور SQL فراهم نیست.
- تست یکپارچه CRUD، RowVersion، پردازش همزمان Bulk، محاسبات و SQL Performance باید پس از Build روی محیط شما انجام شود.
- هیچ Razor Page، Web API Controller، Authentication و Authorization در این فاز اضافه نشده است.
- دو محدودیت مشخص شده برای پاکسازی Batch نیمه کاره و محاسبه گروهی فرمول های غیر جدولی نیاز به تصمیم جداگانه در لایه SQL دارند.
