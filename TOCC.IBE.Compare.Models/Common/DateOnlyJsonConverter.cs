using System;
using Newtonsoft.Json;

namespace TOCC.IBE.Compare.Models.Common
{
    /// <summary>
    /// JSON converter that formats DateTime values as "yyyy-MM-dd" strings for API calls.
    /// Handles both serialization (DateTime to string) and deserialization (string to DateTime).
    /// </summary>
    public class DateOnlyJsonConverter : JsonConverter<DateTime?>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override void WriteJson(JsonWriter writer, DateTime? value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                writer.WriteValue(value.Value.ToString(DateFormat));
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override DateTime? ReadJson(JsonReader reader, Type objectType, DateTime? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            var stringValue = reader.Value.ToString();
            if (string.IsNullOrEmpty(stringValue))
                return null;

            if (DateTime.TryParseExact(stringValue, DateFormat, null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fallback to standard DateTime parsing if the exact format doesn't match
            if (DateTime.TryParse(stringValue, out var fallbackResult))
            {
                return fallbackResult;
            }

            throw new JsonSerializationException($"Unable to parse '{stringValue}' as DateTime. Expected format: {DateFormat}");
        }
    }
}
