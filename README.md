# TIR

A system for managing employees, their dynamic attributes, and bulk processing.

This project provides a web-based interface for managing employee data, including their fixed properties and a flexible set of dynamic attributes. It also offers functionality for bulk data operations and advanced search capabilities. Users can manage attributes, view employee details, and perform large-scale data updates.

## Tech Stack

* .NET 8.0
* ASP.NET Core
* C#
* Razor Pages
* Bootstrap 5
* jQuery
* Hangfire (configured but disabled by default)

## Quick Start

To run this project, you will need to have the .NET SDK installed.

1. **Clone the repository:**
 ```bash
 git clone <repository-url>
 cd TIR
 ```
2. **Configure connection strings:**
 Update the `ConnectionStrings` section in `src/TIR.Web/appsettings.json` with your database connection details.
3. **Run the application:**
 ```bash
 dotnet run --project src/TIR.Web/TIR.Web.csproj
 ```

See the project manifest for specific build and run commands.

## Project Structure

```
.
├── src
│ ├── TIR.Application
│ ├── TIR.Domain
│ ├── TIR.Infrastructure
│ └── TIR.Web
│ ├── Pages
│ │ ├── Shared
│ │ ├── Attributes
│ │ ├── Bulk
│ │ └── Employees
│ ├── Properties
│ ├── wwwroot
│ │ ├── css
│ │ └── js
│ └── appsettings.json
└── TIR.sln
```

## Key Concepts

* **Employees:** Core entities representing individuals within an organization. Managed via `src/TIR.Web/Pages/Employees/`.
* **Attributes:** Dynamic properties that can be defined and assigned to employees. These allow for flexible data modeling beyond fixed fields. Managed via `src/TIR.Web/Pages/Attributes/`.
* **Bulk Operations:** Functionality to process large amounts of data, such as employee attribute updates, via CSV files. Managed via `src/TIR.Web/Pages/Bulk/`.
* **Razor Pages:** The web UI is built using ASP.NET Core Razor Pages, providing a page-centric model for server-side rendering.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
