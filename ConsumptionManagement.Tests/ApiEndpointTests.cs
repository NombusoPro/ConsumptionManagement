using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConsumptionManagement.API.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsumptionManagement.Tests
{
    public class ApiEndpointTests : IClassFixture<ApiFactory>
    {
        private readonly HttpClient _client;

        public ApiEndpointTests(ApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Sites_can_be_created_and_listed()
        {
            var create = await _client.PostAsJsonAsync("/api/sites", new { name = "North Plant", address = "1 Energy Road" });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var created = await create.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("North Plant", created.GetProperty("name").GetString());

            var list = await _client.GetFromJsonAsync<JsonElement>("/api/sites");
            Assert.Contains(list.EnumerateArray(), site => site.GetProperty("name").GetString() == "North Plant");
        }

        [Fact]
        public async Task Create_site_rejects_a_blank_name()
        {
            var response = await _client.PostAsJsonAsync("/api/sites", new { name = "   ", address = "Somewhere" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Meters_require_an_existing_site_and_can_be_listed()
        {
            var missing = await _client.PostAsJsonAsync("/api/sites/99999/meters", new { meterNumber = "MTR-MISSING" });
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Contains("was not found", await missing.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

            var siteId = await CreateSiteAsync("Meter Site");
            var created = await _client.PostAsJsonAsync($"/api/sites/{siteId}/meters", new { meterNumber = "MTR-2002" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var list = await _client.GetFromJsonAsync<JsonElement>($"/api/sites/{siteId}/meters");
            Assert.Equal("MTR-2002", list.EnumerateArray().Single().GetProperty("meterNumber").GetString());

            var unknownList = await _client.GetAsync("/api/sites/99999/meters");
            Assert.Equal(HttpStatusCode.NotFound, unknownList.StatusCode);
        }

        [Fact]
        public async Task Duplicate_meter_number_returns_409()
        {
            var response = await _client.PostAsJsonAsync("/api/sites/1/meters", new { meterNumber = "MTR-1001" });
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("meter number already exists", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Readings_reject_missing_meters_non_utc_timestamps_and_duplicates()
        {
            var missing = await _client.PostAsJsonAsync("/api/meters/99999/readings", new
            {
                timestampUtc = "2026-03-01T00:00:00Z",
                cumulativeKwh = 10
            });
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

            var meterId = await CreateMeterAsync("MTR-READ-1");

            var local = await _client.PostAsJsonAsync($"/api/meters/{meterId}/readings", new
            {
                timestampUtc = "2026-03-01T00:00:00",
                cumulativeKwh = 10
            });
            Assert.Equal(HttpStatusCode.BadRequest, local.StatusCode);
            Assert.Contains("UTC ISO 8601", await local.Content.ReadAsStringAsync(), StringComparison.Ordinal);

            var offset = await _client.PostAsJsonAsync($"/api/meters/{meterId}/readings", new
            {
                timestampUtc = "2026-03-01T00:00:00+02:00",
                cumulativeKwh = 10
            });
            Assert.Equal(HttpStatusCode.BadRequest, offset.StatusCode);

            var created = await _client.PostAsJsonAsync($"/api/meters/{meterId}/readings", new
            {
                timestampUtc = "2026-03-01T00:00:00Z",
                cumulativeKwh = 10.5
            });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            var duplicate = await _client.PostAsJsonAsync($"/api/meters/{meterId}/readings", new
            {
                timestampUtc = "2026-03-01T00:00:00+00:00",
                cumulativeKwh = 11
            });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Contains("already exists for this meter", await duplicate.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Readings_are_ordered_and_filtered_inclusively()
        {
            var meterId = await CreateMeterAsync("MTR-FILTER");
            await PostReadingAsync(meterId, "2026-04-02T00:00:00Z", 30);
            await PostReadingAsync(meterId, "2026-04-01T00:00:00Z", 10);
            await PostReadingAsync(meterId, "2026-04-01T12:00:00Z", 20);

            var filtered = await _client.GetFromJsonAsync<JsonElement>(
                $"/api/meters/{meterId}/readings?fromUtc={Uri.EscapeDataString("2026-04-01T00:00:00Z")}&toUtc={Uri.EscapeDataString("2026-04-01T12:00:00Z")}");

            var timestamps = filtered.EnumerateArray().Select(reading => reading.GetProperty("timestampUtc").GetString()).ToArray();
            Assert.Equal(
                ["2026-04-01T00:00:00Z", "2026-04-01T12:00:00Z"],
                timestamps);

            var reversed = await _client.GetAsync(
                $"/api/meters/{meterId}/readings?fromUtc={Uri.EscapeDataString("2026-04-02T00:00:00Z")}&toUtc={Uri.EscapeDataString("2026-04-01T00:00:00Z")}");
            Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        }

        [Fact]
        public async Task Summary_validates_the_range_and_uses_the_consumption_calculator()
        {
            var missingParams = await _client.GetAsync("/api/meters/1/summary");
            Assert.Equal(HttpStatusCode.BadRequest, missingParams.StatusCode);

            var missingMeter = await _client.GetAsync(
                $"/api/meters/99999/summary?fromUtc={Uri.EscapeDataString("2026-05-01T00:00:00Z")}&toUtc={Uri.EscapeDataString("2026-05-02T00:00:00Z")}");
            Assert.Equal(HttpStatusCode.NotFound, missingMeter.StatusCode);

            var meterId = await CreateMeterAsync("MTR-SUMMARY");
            await PostReadingAsync(meterId, "2026-05-01T23:00:00Z", 10);
            await PostReadingAsync(meterId, "2026-05-02T01:00:00Z", 15);
            await PostReadingAsync(meterId, "2026-05-02T06:00:00Z", 14);

            var summary = await _client.GetFromJsonAsync<JsonElement>(
                $"/api/meters/{meterId}/summary?fromUtc={Uri.EscapeDataString("2026-05-01T23:00:00Z")}&toUtc={Uri.EscapeDataString("2026-05-02T06:00:00Z")}");

            Assert.Equal(4m, summary.GetProperty("totalConsumptionKwh").GetDecimal());
            var daily = summary.GetProperty("dailyConsumption").EnumerateArray().Single();
            Assert.Equal("2026-05-02", daily.GetProperty("date").GetString());
            Assert.Equal(5m, daily.GetProperty("consumptionKwh").GetDecimal());
        }

        [Fact]
        public async Task Summary_uses_only_readings_inside_the_requested_range()
        {
            var meterId = await CreateMeterAsync("MTR-RANGE");
            await PostReadingAsync(meterId, "2026-01-11T00:00:00Z", 10);
            await PostReadingAsync(meterId, "2026-04-01T00:00:00Z", 100);
            await PostReadingAsync(meterId, "2026-04-05T00:00:00Z", 180);
            await PostReadingAsync(meterId, "2026-04-10T00:00:00Z", 180);
            await PostReadingAsync(meterId, "2026-04-12T08:30:00Z", 200);
            await PostReadingAsync(meterId, "2026-04-12T13:00:00Z", 300);

            var april = await _client.GetFromJsonAsync<JsonElement>(
                $"/api/meters/{meterId}/summary?fromUtc={Uri.EscapeDataString("2026-04-01T00:00:00Z")}&toUtc={Uri.EscapeDataString("2026-04-13T00:00:00Z")}");

            Assert.Equal(200m, april.GetProperty("totalConsumptionKwh").GetDecimal());
            var aprilDays = april.GetProperty("dailyConsumption").EnumerateArray().ToArray();
            Assert.Equal(2, aprilDays.Length);
            Assert.Equal("2026-04-05", aprilDays[0].GetProperty("date").GetString());
            Assert.Equal(80m, aprilDays[0].GetProperty("consumptionKwh").GetDecimal());
            Assert.Equal("2026-04-12", aprilDays[1].GetProperty("date").GetString());
            Assert.Equal(120m, aprilDays[1].GetProperty("consumptionKwh").GetDecimal());

            var fullSpan = await _client.GetFromJsonAsync<JsonElement>(
                $"/api/meters/{meterId}/summary?fromUtc={Uri.EscapeDataString("2026-01-01T00:00:00Z")}&toUtc={Uri.EscapeDataString("2026-04-13T00:00:00Z")}");

            Assert.Equal(290m, fullSpan.GetProperty("totalConsumptionKwh").GetDecimal());
            var fullDays = fullSpan.GetProperty("dailyConsumption").EnumerateArray().ToArray();
            Assert.Equal(
                ["2026-04-01", "2026-04-05", "2026-04-12"],
                fullDays.Select(day => day.GetProperty("date").GetString()).ToArray());
            Assert.Equal(90m, fullDays[0].GetProperty("consumptionKwh").GetDecimal());
            Assert.Equal(80m, fullDays[1].GetProperty("consumptionKwh").GetDecimal());
            Assert.Equal(120m, fullDays[2].GetProperty("consumptionKwh").GetDecimal());
        }

        [Fact]
        public async Task Unique_constraint_violations_are_returned_as_409()
        {
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/meters/1/readings";
            context.Response.Body = new MemoryStream();

            var exception = new DbUpdateException(
                "duplicate key",
                new SqliteException("UNIQUE constraint failed: MeterReadings.MeterId, MeterReadings.TReadingimeUtc"));

            var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            Assert.True(handled);
            Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

            context.Response.Body.Position = 0;
            using var document = await JsonDocument.ParseAsync(context.Response.Body);
            Assert.Contains(
                "already exists for this meter",
                document.RootElement.GetProperty("detail").GetString(),
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task<int> CreateSiteAsync(string name)
        {
            var response = await _client.PostAsJsonAsync("/api/sites", new { name });
            response.EnsureSuccessStatusCode();
            var site = await response.Content.ReadFromJsonAsync<JsonElement>();
            return site.GetProperty("id").GetInt32();
        }

        private async Task<int> CreateMeterAsync(string meterNumber)
        {
            var siteId = await CreateSiteAsync(meterNumber + " site");
            var response = await _client.PostAsJsonAsync($"/api/sites/{siteId}/meters", new { meterNumber });
            response.EnsureSuccessStatusCode();
            var meter = await response.Content.ReadFromJsonAsync<JsonElement>();
            return meter.GetProperty("id").GetInt32();
        }

        private async Task PostReadingAsync(int meterId, string timestampUtc, decimal cumulativeKwh)
        {
            var response = await _client.PostAsJsonAsync($"/api/meters/{meterId}/readings", new { timestampUtc, cumulativeKwh });
            response.EnsureSuccessStatusCode();
        }
    }

    file sealed class SqliteException : Exception
    {
        public SqliteException(string message) : base(message)
        {
        }
    }
}
