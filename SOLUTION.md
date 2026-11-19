# Solution

## Design

The API is a small ASP.NET Core 8 service with three controllers over one EF Core `DbContext`:

- `SitesController` creates and lists sites.
- `MetersController` creates and lists meters for a site.
- `ReadingsController` creates and lists cumulative readings and returns a consumption summary.

Sites, meters, and readings already existed as EF models. The public contracts use clearer names than the stored columns: requests and responses say `timestampUtc` and `cumulativeKwh`, which map to `MeterReading.TReadingimeUtc` and `MeterReading.ReadingSum`. Renaming those columns would have meant a migration and a break with the existing seed data, so the mapping stays at the API boundary.

`ConsumptionCalculator` is a pure function. It receives readings plus `fromUtc` and `toUtc`, keeps timestamps inside the inclusive window, and returns one summary DTO. The summary endpoint loads that window from SQL and calls the calculator. The calculator filters again so the same rules hold if a caller passes a wider set. Total consumption and the daily rows are produced from that one ordered list, so they cannot drift apart by using different readings.

Daily consumption walks consecutive readings in timestamp order. A positive difference is added to the UTC calendar date of the later reading. A difference of zero or less is ignored, and a date that receives nothing is left out of the list. Two readings on the same UTC date both contribute to that date. A pair that crosses midnight contributes to the later date only.

Meter numbers are unique. Reading timestamps are unique per meter, both by a query before insert and by the unique index `(MeterId, TReadingimeUtc)`. A unique-index failure from SQL Server (error 2601 or 2627) is translated to HTTP 409 by the global exception handler, which covers a race the pre-check can miss.

Timestamps are accepted only as UTC ISO 8601 text ending in `Z` or `+00:00`. Other offsets and values with no offset are 400. Query bounds use the same parser, so a local offset cannot shift the window.

Invalid models use the framework validation problem response. Missing sites and meters are 404. Unexpected exceptions are logged and returned as a short problem payload without the exception text.

## Trade-offs

Controllers talk to `DbContext` directly. A repository would have repeated the same queries without adding a second persistence implementation. The consumption rules stay out of the controller in `ConsumptionCalculator`, which is the part that is easy to get wrong.

A decreasing cumulative value is not treated as a meter reset. Total consumption is still latest minus earliest, so it can be negative. Daily rows drop that negative step. After a reset, the daily sum and the total can disagree. Splitting the series at the reset would hide the drop from the total as well, and the required total formula does not do that.

Development applies EF migrations on startup so LocalDB is ready without a manual `dotnet ef database update`. Tests replace SQL Server with SQLite in memory and set the environment to `Testing`, which skips HTTPS redirection and startup migrations. SQLite checks the unique index; it does not reproduce SQL Server error numbers, so the 409 mapping for those numbers is also covered by a handler test with a unique-constraint exception.

The `Testing` environment flag is the only production-pipeline branch added for tests.

## AI use

I used Cursor’s coding agent (Grok) in this repository.

I used it to implement `ConsumptionCalculator` from the written boundary rules, then the site, meter, and reading endpoints, UTC validation, and the 409 handler. I also used it to add the in-memory API tests and to compare a live summary response with the `MeterReadings` rows in LocalDB.

One suggestion I did not take was to count a daily pair only when both readings fall on that same UTC date. That reading of the requirement would drop the 80 kWh from 1 April to 5 April and the 20 kWh from 10 April to 12 April, leaving 100 kWh of daily consumption against a 200 kWh total. The requirement says to order all in-range readings, difference each consecutive pair, and attribute the difference to the later reading’s date. Cross-day pairs stay in the breakdown for that reason.

I checked the generated code with the xUnit suite above, including the duplicate-timestamp, bad-timestamp, empty-range, and cross-day cases. I also queried meter 9 in LocalDB and called the summary endpoint, and checked that 5 April’s 80 kWh is 180 − 100 and that 12 April’s 120 kWh is (200 − 180) + (300 − 200).
