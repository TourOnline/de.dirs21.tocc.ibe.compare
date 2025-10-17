using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
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

            var comparer = new AvailabilityComparer();
            
            // Configure paths to skip during comparison
            comparer.SkipPaths = new System.Collections.Generic.List<string>
            {
                // Example: Skip specific properties by path
                "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinStayThrough",
                 "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinLos",
                 "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsCta",
                 "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsCtd",
                 "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.IsMixable",
            };

            // Act
            bool areEqual = comparer.Compare(v1Response, v2Response);

            var differences = comparer.Differences;
            
            // Save differences to file
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var resultsFolder = Path.Combine(baseDir, "..", "..", "..", "TestResults", "Differences");
            Directory.CreateDirectory(resultsFolder);
            var outputFile = Path.Combine(resultsFolder, $"Differences_{timestamp}.txt");
            
            using (var writer = new StreamWriter(outputFile))
            {
                writer.WriteLine($"=== Comparison Results ===");
                writer.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Total Differences Found: {differences.Count}");
                writer.WriteLine();
                
                foreach (var d in differences)
                {
                    writer.WriteLine($"[{d.Type}] {d.Path}");
                    writer.WriteLine($"  Expected (V1): {d.Expected}");
                    writer.WriteLine($"  Actual (V2):   {d.Actual}");
                    writer.WriteLine();
                }
            }
            
            System.Console.WriteLine($"\n=== Differences saved to: {outputFile} ===");
            
            // Print differences for diagnostics
            System.Console.WriteLine($"\n=== Total Differences Found: {differences.Count} ===\n");
            foreach (var d in differences)
            {
                System.Console.WriteLine($"[{d.Type}] {d.Path}");
                System.Console.WriteLine($"  Expected (V1): {d.Expected}");
                System.Console.WriteLine($"  Actual (V2):   {d.Actual}");
                System.Console.WriteLine();
            }

            // Assert - V1 and V2 have structural differences, so they should NOT be equal
            Assert.False(areEqual, "V1 and V2 APIs have different structures - differences are expected");
            
            // Validate that we're detecting differences (comparison is working)
            Assert.NotEmpty(differences);
            
            // Validate we're finding both MissingInV1 and MissingInV2 differences
            var missingInV1 = differences.Count(d => d.Type == TOCC.IBE.Compare.Models.Core.DifferenceType.MissingInV1);
            var missingInV2 = differences.Count(d => d.Type == TOCC.IBE.Compare.Models.Core.DifferenceType.MissingInV2);
            var valueMismatches = differences.Count(d => d.Type == TOCC.IBE.Compare.Models.Core.DifferenceType.ValueMismatch);
            
            System.Console.WriteLine($"\n=== Difference Summary ===");
            System.Console.WriteLine($"MissingInV1: {missingInV1}");
            System.Console.WriteLine($"MissingInV2: {missingInV2}");
            System.Console.WriteLine($"ValueMismatch: {valueMismatches}");
            System.Console.WriteLine($"Total: {differences.Count}");
            
            Assert.True(missingInV1 > 0, "Should find properties that exist in V2 but not in V1");
            Assert.True(missingInV2 > 0, "Should find properties that exist in V1 but not in V2");
            
            System.Console.WriteLine($"\n✅ Test passed: Comparison engine correctly detected {differences.Count} differences between V1 and V2 APIs");
        }
    }
}
