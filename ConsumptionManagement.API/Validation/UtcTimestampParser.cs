using System.Globalization;
using System.Text.RegularExpressions;

namespace ConsumptionManagement.API.Validation
{
    public static partial class UtcTimestampParser
    {
        public const string ErrorMessage =
            "Timestamp must be a UTC ISO 8601 value (for example 2026-01-01T00:00:00Z).";

        [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|\+00:00)$", RegexOptions.CultureInvariant)]
        private static partial Regex UtcIso8601Pattern();

        public static bool TryParse(string? text, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(text) || !UtcIso8601Pattern().IsMatch(text))
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var parsed)
                || parsed.Offset != TimeSpan.Zero)
            {
                return false;
            }

            utc = parsed.UtcDateTime;
            return true;
        }
    }
}
