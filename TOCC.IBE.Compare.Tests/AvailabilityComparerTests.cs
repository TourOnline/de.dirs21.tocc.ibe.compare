using Newtonsoft.Json;
using System.IO;
using System.Text.Json;
using TOCC.Core.Models;
using TOCC.IBE.Compare.Models.V1;
using Xunit;

namespace TOCC.IBE.Compare.Tests
{
    public class AvailabilityComparerTests
    {
        [Fact]
        public void Compare_NewAndOldJson_ReturnsExpectedResult()
        {
            // Arrange
            var baseDir = System.AppContext.BaseDirectory;
            var newJsonPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "TOCC.IBE.Compare", "Test", "new.json"));
            var oldJsonPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "TOCC.IBE.Compare", "Test", "old.json"));

            var newJson = File.ReadAllText(newJsonPath);
            var oldJson = File.ReadAllText(oldJsonPath);

            var v1Response = JsonConvert.DeserializeObject<V1Response>(oldJson);
            var v2Response = JsonConvert.DeserializeObject<TOCC.Contracts.IBE.Models.Availability.Response>(newJson);

            var rules = new ComparisonRules();
            var comparer = new AvailabilityComparer(rules);

            // Act
            bool areEqual = comparer.Compare(v1Response, v2Response);

            var differences = comparer.Differences;
            // Print differences for diagnostics (look for ".Cycle" entries)
            foreach (var d in differences)
            {
                System.Console.WriteLine($"Difference: Path={d.Path}, Expected={d.Expected}, Actual={d.Actual}");
            }

            // Assert
            Assert.True(areEqual); // Change to Assert.False if you expect differences
        }
    }
}
