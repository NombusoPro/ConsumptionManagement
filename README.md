# Consumption Management API

ASP.NET Core 8 API for energy sites, meters, cumulative readings, and consumption summaries.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (included with Visual Studio) or another SQL Server instance

In Development the API uses LocalDB:

`Server=(localdb)\MSSQLLocalDB;Database=ConsumptionDb;Integrated Security=True;TrustServerCertificate=True`

That connection string is in `ConsumptionManagement.API/appsettings.Development.json`. On startup in Development, Entity Framework applies migrations and creates `ConsumptionDb` if needed.

`appsettings.json` points at SQL Server on `localhost,1433` (`sa` / `StrongPassword123!`). Use that file when `ASPNETCORE_ENVIRONMENT` is not `Development`.

## Run the API

From the repository root:

```bash
dotnet run --project ConsumptionManagement.API --launch-profile https
```

- Swagger UI: https://localhost:7092/swagger

The development HTTPS certificate is the local ASP.NET Core dev cert. Trust it once with `dotnet dev-certs https --trust` if the browser warns you.

## Run the tests

```bash
dotnet test ConsumptionManagement.Tests
```

Tests host the API with `WebApplicationFactory` and SQLite in memory. They do not need LocalDB or the SQL Server container.
