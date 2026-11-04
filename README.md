# TIR — Dynamic Personnel Information Management

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Razor_Pages-512BD4)
![SQL Server](https://img.shields.io/badge/Database-Microsoft_SQL_Server-CC2927)
![Status](https://img.shields.io/badge/Status-MVP-orange)

**TIR** is a Persian-language, right-to-left personnel management web application built with **ASP.NET Core 8 Razor Pages**. It combines core employee records with administrator-defined attributes, typed values, advanced search, formula-based calculations, and Excel-based bulk operations.

The application uses a layered .NET solution and delegates data operations and business rules to **Microsoft SQL Server stored procedures**. Its interface is designed for Iranian users, including Jalali (Persian) date input/display and Tehran-local time handling.

> [!IMPORTANT]
> **Database setup is required.** `database/DataBase.sql` is a reference schema export, not an incremental upgrade script. After the existing schema updates through version `018` are applied, execute [`database/migrations/019_formula_bulk_scalability.sql`](database/migrations/019_formula_bulk_scalability.sql) against `Test` **before** deploying this update. It upgrades the formula set calculator and the bulk formula worker and records schema version `019`.

## Contents

- [Features](#features)
- [Technology stack](#technology-stack)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [Configuration](#configuration)
- [Using TIR](#using-tir)
- [Excel bulk wizard](#excel-bulk-wizard)
- [Legacy CSV format](#bulk-csv-format)
- [Background processing](#background-processing)
- [Database integration](#database-integration)
- [Development notes](#development-notes)

## Features

| Area | Functionality |
| --- | --- |
| **Employee records** | Create employees with optional initial attributes; list, filter, sort, edit, view details, soft-delete, and restore records. |
| **Dynamic attributes** | Define attributes without changing the employee form's C# model; control labels, order, required status, and value types. |
| **Selectable options** | Manage the options for single-select attributes, including editing, ordering, and enabling/disabling options. |
| **Typed values** | Store text, integers, decimals, dates, times, date-times, booleans, and selected option IDs. |
| **Advanced search** | Search fixed employee fields and dynamic attributes; combine conditions with **AND** inside a group and **OR** across groups; paginate and sort results. |
| **Calculated attributes** | Register SQL-backed formulas and dependencies, calculate values on demand, and track valid, stale, and failed results. |
| **Bulk processing** | Three-step wizard for Excel employee creation/update or selected formula calculations; stage changes with `SqlBulkCopy`, process sequential 10,000-row Hangfire jobs, and inspect cumulative progress and per-row errors. |
| **Concurrency control** | Use SQL Server `rowversion` values when updating employee records, attributes, options, and existing values. |
| **Background jobs** | Optionally run formula maintenance and bulk-staging cleanup through Hangfire. |
| **Localized UI** | Persian RTL layout, Jalali date conversion, and an Iran clock. |

## Technology stack

- **Runtime:** .NET 8 / C# / ASP.NET Core Razor Pages
- **Database:** Microsoft SQL Server
- **Data access:** Dapper, `Microsoft.Data.SqlClient`, stored procedures, table-valued parameters, and `SqlBulkCopy`
- **Validation:** FluentValidation
- **Object mapping:** Mapperly
- **Background jobs:** Hangfire with SQL Server storage (required for the new bulk wizard)
- **Logging:** Serilog (console and HTTP request logging)
- **Frontend:** Razor, Bootstrap RTL, jQuery, JavaScript, and CSS
- **Localization:** Persian calendar, Tehran time zone, Persian-language interface

Package versions are managed centrally in [`Directory.Packages.props`](Directory.Packages.props), while the shared target framework and compiler settings are defined in [`Directory.Build.props`](Directory.Build.props).

## Architecture

The solution follows a layered separation of concerns:

```text
TIR.sln
├── Directory.Build.props
├── Directory.Packages.props
└── src/
    ├── TIR.Domain/          # Employee domain model
    ├── TIR.Application/     # Requests, services, validation, contracts
    ├── TIR.Infrastructure/  # Dapper/SQL Server repositories and bulk staging
    └── TIR.Web/             # Razor Pages, UI helpers, JS/CSS, Hangfire jobs
```

**Request flow:** Razor Page → application service / validator → repository interface → infrastructure repository → SQL Server stored procedure.

`TIR.Web` registers the application and infrastructure services through dependency injection. The infrastructure layer opens SQL connections per operation. Read-only stored-procedure calls may retry SQL Server deadlocks. The bulk employee writer additionally retries only error `1205`, using a database procedure designed to safely reprocess rolled-back sub-batches; other writes are not automatically retried.

The database is responsible for important validation and transactional behavior, so the application depends on its expected SQL contracts.

## Getting started

### Prerequisites

1. **.NET 8 SDK** installed and available as `dotnet`.
2. A reachable **SQL Server** instance with the TIR schema, tables, table types, and stored procedures already deployed.
3. A database login with the permissions needed to execute the stored procedures and perform bulk staging.
4. Network access for NuGet package restore and the frontend CDN assets.

### 1. Clone and build

```bash
git clone https://github.com/Amir-ESH/TIR.git
cd TIR
dotnet restore TIR.sln
dotnet build TIR.sln --configuration Release
```

### 2. Configure the database connection

Create `src/TIR.Web/appsettings.Development.json` locally (this path is already git-ignored):

```json
{
  "ConnectionStrings": {
    "Test": "Server=localhost;Database=TIR;User Id=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True;"
  },
  "Hangfire": {
    "Enabled": false
  }
}
```

Replace the sample values with your SQL Server connection details. `TrustServerCertificate=True` is appropriate only for local development when you explicitly trust the server; use properly validated TLS certificates in deployed environments.

The `ConnectionStrings:Test` setting is **required** even when Hangfire is disabled. Leaving it empty causes startup to fail during dependency registration.

You can also supply configuration using environment variables such as `ConnectionStrings__Test`, which is preferable to committing credentials.

### 3. Run the web application

```bash
dotnet run --project src/TIR.Web/TIR.Web.csproj --launch-profile TIR.Web
```

The included launch profile configures:

- HTTPS: <https://localhost:49982>
- HTTP: <http://localhost:49983> (normally redirected to HTTPS)

If your development HTTPS certificate is not trusted, follow your operating system's .NET development-certificate instructions (`dotnet dev-certs https`).

> [!NOTE]
> The app can start only if the required database connection is configured. Pages that query or update records additionally require the matching database objects and stored procedures. There is no bundled database setup script or sample seed data.

## Configuration

The default configuration in [`src/TIR.Web/appsettings.json`](src/TIR.Web/appsettings.json) leaves both connection strings empty and Hangfire disabled.

| Setting | Purpose | Default in repository |
| --- | --- | --- |
| `ConnectionStrings:Test` | Main SQL Server connection used by the repositories | Empty; **must configure** |
| `ConnectionStrings:Hangfire` | SQL Server connection for Hangfire storage | Empty |
| `Hangfire:Enabled` | Register Hangfire and its server | `false` |
| `Hangfire:AutoCreateSchema` | Allow Hangfire to prepare its SQL schema | `false` when unset |
| `Hangfire:Jobs:Enabled` | Register the recurring jobs when Hangfire is enabled | `true` when unset |

When enabling Hangfire, supply its connection string and ensure its tables/schema already exist, or explicitly opt into schema creation with the appropriate database permissions. The `/hangfire` dashboard is mapped **only in Development** when Hangfire is enabled; its built-in default authorization is intended for local access.

## Using TIR

The main pages in the web interface are:

| Route | Description |
| --- | --- |
| `/` | Dashboard with employee, attribute, and active-formula counts. |
| `/Employees` | Browse, search, sort, soft-delete, and restore employees. |
| `/Employees/Create` | Create an employee and enter available initial attribute values. |
| `/Employees/Details/{id}` | Inspect an employee and their dynamic values; trigger formula recalculation. |
| `/Employees/Edit/{id}` | Update fixed fields and save individual dynamic values. |
| `/Employees/Search` | Build grouped attribute filters and run advanced searches. |
| `/Attributes` | Browse, create, edit, soft-delete, and restore attribute definitions. |
| `/Attributes/Options/{id}` | Manage options for a single-select attribute. |
| `/Bulk` | Excel create/update wizard or formula calculation with employee selection and a live progress report. |
| `/Bulk/Report/{id}` | View batch counts and errors, or delete staged batch data. |

### Dynamic attribute types

The `DataKind` values used throughout the application are:

| Code | Type | Example |
| --- | --- | --- |
| `1` | String | Job title |
| `2` | Integer | Years of service |
| `3` | Decimal | Salary or an amount |
| `4` | Date | Date of hire |
| `5` | Time | Shift start time |
| `6` | Date-time | Appointment timestamp |
| `7` | Boolean | Active certification |
| `8` | Single select | Department option |

`SourceKind = 1` denotes a user-entered value, while `SourceKind = 2` denotes a calculated attribute. Formula executors and dependency rules are registered through the application/SQL contracts; there is no general-purpose formula editor in the current Razor Pages UI.

For advanced searches, supported operators include equality, inequality, comparisons, ranges, and text containment where appropriate to the data type. Conditions within a numbered group are combined using `AND`, and the groups are combined using `OR`.

## Excel bulk wizard

The `/Bulk` page is now a three-step wizard: choose **create**, **update**, or **calculate formula**; upload a freshly generated `.xlsx` template or select formula targets using advanced search; review and submit to sequential Hangfire jobs. A cumulative progress bar and row-level errors are displayed on the third step. Data remains staged until execution is confirmed.

For updates, download an employee workbook from `/Employees/Index` or `/Employees/Search`. It includes the employee `RowVersion`, an individual version column per editable attribute, and an `OriginalValues` worksheet. Only attribute values changed from that worksheet are sent to SQL. Blank changed cells clear optional values; required attributes cannot be cleared. The SQL procedures retain business validation and concurrency control.

Hangfire must be enabled to execute wizard requests. **Employee import no longer has an application-level 50,000-row limit.** Each Hangfire job still covers 10,000 employees, but SQL Server processes employee writes in batches of 1,000 rows and formula calculations in batches of 500 employees. Formula progress counts are now updated incrementally instead of recounting every employee after each batch. Formula jobs retry transient SQL deadlocks and timeouts on the current small batch. The separate Excel *export* limit remains 50,000 employees. A single Excel worksheet is inherently limited to 1,048,576 rows, and imports are also constrained by available server memory, storage, request timeouts and any reverse proxy limits. For reliable large imports, run the application in 64-bit mode with sufficient resources. Apply database migration `019` first; otherwise the previous 10,000-employee SQL formula transaction remains unchanged.

## Bulk CSV format

The following describes the earlier CSV contract, retained in the backend for compatibility. The `/Bulk` page now presents the Excel wizard instead.

The **Bulk** page accepts a **UTF-8 CSV** with this exact column order:

```csv
EmployeeId,AttributeId,OperationKind,Value,ExpectedRowVersion
102,5,1,4500000.000,
103,5,1,5200000.000,
```

These IDs and values are illustrative. Use IDs and compatible types from your own database.

| Column | Meaning |
| --- | --- |
| `EmployeeId` | Existing employee ID. |
| `AttributeId` | Target dynamic attribute ID. |
| `OperationKind` | `1` saves a value; `2` clears a value. |
| `Value` | Value formatted according to the attribute's `DataKind`; blank when clearing. |
| `ExpectedRowVersion` | Base64 encoding of the existing value's **8-byte** SQL `rowversion` for updates; leave empty when inserting a new value. |

**Important rules:**

- Keep all rows for the same employee **contiguous**, preferably sorted by `EmployeeId`. The CSV reader groups consecutive rows into one employee input.
- Each non-empty data row must have **exactly five columns**. The reader supports quoted fields and ignores blank lines and lines starting with `#`.
- Numeric values use invariant formatting (for example, `4500000.000`). Dates can use `yyyy-MM-dd` or a Jalali date such as `1405/08/09`; date-times can also be entered in Tehran-local Jalali format with a time.
- Existing value updates require the correct `ExpectedRowVersion`; otherwise SQL may report a concurrency conflict. The sample empty versions above apply only to **new value inserts**.
- The upload page limits files to **250 MiB**. Transfer batches and SQL transaction sizes can each be configured between `100` and `10,000`.
- The process stages rows with `SqlBulkCopy`, invokes the database batch processor, then displays requested, succeeded, and failed employee counts plus error details. The UI shows up to the first 200 errors.
- Processing happens during the HTTP request; keep the browser session open until the report is returned.

## Background processing

When enabled, Hangfire registers two recurring jobs using the **Tehran time zone**:

| Job ID | Schedule | Purpose |
| --- | --- | --- |
| `tir-formula-midnight-iran` | Daily at **00:00 Tehran time** | Mark expired formula results stale, then recalculate eligible formulas in dependency order. |
| `tir-bulk-cleanup-hourly` | Hourly | Remove completed bulk batches older than **24 hours**. |

Both jobs are disabled unless `Hangfire:Enabled` is set to `true`; recurring registration can be suppressed separately with `Hangfire:Jobs:Enabled = false`.

## Database integration

The infrastructure project calls SQL Server stored procedures under the `dbo` schema. Representative contracts include:

| Domain | Stored procedures / objects (examples) |
| --- | --- |
| Employees | `SP_Employee_Create`, `SP_Employee_Update`, `SP_Employee_List`, `SP_Employee_SearchAdvanced`, `SP_Employee_SoftDelete`, `SP_Employee_Restore` |
| Attributes | `SP_AttributeDefinition_Create`, `SP_AttributeDefinition_List`, `SP_AttributeOption_Create`, `SP_AttributeOption_Update`, `SP_AttributeOption_SetEnabled` |
| Values | `SP_EmployeeAttributeValue_Get`, `SP_EmployeeAttributeValue_List`, `SP_EmployeeAttributeValue_Save` |
| Formulas | `SP_AttributeFormula_Register`, `SP_FormulaDependency_Add`, `SP_Formula_GetCalculationOrder`, `SP_Formula_CalculateAll`, `SP_Formula_CalculateSet`, `SP_Formula_MarkExpired` |
| Bulk operations | `SP_Batch_Create`, `SP_Batch_Process`, `SP_Batch_GetReport`, `SP_Batch_Delete`, `SP_Batch_Cleanup`; staging tables `dbo.EmployeeInput`, `dbo.ValueInput` |
| Initial employee values | User-defined table type `dbo.EmployeeNewValueType` |

This is a **code-based integration inventory**, not a complete SQL deployment script. The stored procedures and tables must follow the parameter, result-set, and `rowversion` contracts expected by the C# repositories. In particular, the attribute-option update/state procedures are referenced in code and marked with TODO comments for their SQL implementation.

## Development notes

- **Project maturity:** The interface identifies this as an MVP. No automated test project, database migration set, deployment manifest, or license file is present in this repository snapshot.
- **Security:** The current `Program.cs` does not configure application authentication or authorization for personnel-management pages. **Add access control, database least-privilege, transport security, and operational safeguards before exposing real employee data to the public internet.**
- **Secrets:** Keep SQL credentials out of version control. `appsettings.Development.json` is git-ignored; prefer environment-specific secret management for deployments.
- **Data changes:** Row-version checks help detect stale edits. SQL Server procedures remain the final authority for validation, transactional consistency, and formula dependency behavior.
- **Verification:** Build and runtime behavior must be checked in an environment with the .NET 8 SDK and the required SQL Server objects. The source review used to prepare this document did not execute the application or its database procedures.

---

Source: [Amir-ESH/TIR](https://github.com/Amir-ESH/TIR) · Maintained by [Amir-ESH](https://github.com/Amir-ESH)
