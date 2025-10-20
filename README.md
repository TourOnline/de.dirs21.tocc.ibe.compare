# TOCC IBE API Comparison Tool

Automated integration testing tool that compares IBE (Internet Booking Engine) availability API responses between V1 and V2 implementations to ensure backward compatibility.

## Overview

This tool performs comprehensive comparison testing by:
- Calling both V1 and V2 APIs with identical parameters
- Deep-comparing the JSON responses
- Identifying and reporting any differences
- Generating detailed artifacts for analysis

## Quick Start

### 1. Configure API Endpoints

Edit `TOCC.IBE.Compare.Tests/appsettings.json`:

```json
{
  "IntegrationTest": {
    "Enabled": true,
    "V1BaseUrl": "https://your-v1-api.com",
    "V2BaseUrl": "https://your-v2-api.com",
    "TestCasesFile": "TestData/test-cases.json",
    "ArtifactsFolder": "artifacts"
  }
}
```

### 2. Configure Test Cases

Edit `TOCC.IBE.Compare.Tests/TestData/test-cases.json`:

```json
{
  "Properties": [
    {
      "_oid": "97936",
      "_uuid": "9e0e8dc3-2357-4db1-a283-40d5bc4ffd6b",
      "Description": "Property Name"
    }
  ],
  "QueryConfigurations": [
    {
      "Name": "1 Night - 2 Adults",
      "Parameters": {
        "BuildLevel": "Primary",
        "Los": 1,
        "Occupancy": [ "a,a" ],
        "OutputMode": "Availability",
        "SessionId": "5b04ee87-d123-4ad0-94aa-fcf402533a94",
        "FilterByProducts": [],
        "FilterByLegacyProducts": [],
        "FilterByLegacyTariffs": [],
        "FilterByTariffs": []
      }
    }
  ]
}
```

**Note:** `From` and `Until` dates are automatically generated based on `Los` if not specified.

### 3. Run Tests

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run only integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## How It Works

### Test Case Generation

The tool generates test cases using a **cartesian product** approach:

```
Total Test Cases = (n Properties × m Query Configurations) + n Default Cases
```

**Example:**
- 2 properties × 3 query configurations = 6 test cases
- Plus 2 default cases (one per property)
- **Total: 8 test cases**

### Default Test Case

For each property, a default test case is automatically added:
- **From:** Today
- **Until:** Tomorrow (Today + 1 day)
- **Los:** 1 night
- **Occupancy:** 2 adults (`"a,a"`)

### Random Date Generation

If query configurations specify `Los` but not `From`/`Until`:
- Start date is randomly selected within the next 90 days
- End date is calculated as `From + Los`

## Test Results

### Success Criteria

✅ Test **passes** only when:
- All V1 and V2 responses are identical
- No differences found
- No API errors occurred

❌ Test **fails** when:
- Any differences exist between V1 and V2
- API calls fail
- Deserialization errors occur

### Artifacts

The tool generates detailed artifacts in the `artifacts/` folder:

#### Individual Test Case Files
```
{datetime}_oid{_oid}_uuid{first8chars}_{testname}.json
```

Example:
```
20251020_074500_oid97936_uuid9e0e8dc3_1_Night_-_2_Adults.json
```

Each file contains:
- Query parameters (From, Until, Los, ChannelUuid)
- Full V1 response JSON
- Full V2 response JSON
- List of differences (if any)
- Metadata (timestamp, URLs, success status)

#### Summary File
```
integration-test-summary_{datetime}.json
```

Contains aggregated results for all test cases.

## Configuration Options

### Environment Variables

Override settings using environment variables:

```bash
# PowerShell
$env:IntegrationTest__Enabled="true"
$env:IntegrationTest__V1BaseUrl="https://v1-api.example.com"
$env:IntegrationTest__V2BaseUrl="https://v2-api.example.com"

# Bash
export IntegrationTest__Enabled=true
export IntegrationTest__V1BaseUrl=https://v1-api.example.com
export IntegrationTest__V2BaseUrl=https://v2-api.example.com
```

### Disable Integration Tests

Set `Enabled` to `false` in appsettings or:

```bash
$env:IntegrationTest__Enabled="false"
dotnet test
```

### Skip Specific Properties

Edit `AvailabilityIntegrationTests.cs`, method `ConfigureComparer()`:

```csharp
comparer.SkipPaths = new List<string>
{
    "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinStayThrough",
    "Result.Properties.Periods.Sets.Products.Ticks.Offers.Ticks.MinLos",
    // Add paths to skip
};
```

## Example Output

```
🚀 Starting integration tests
   V1 Base URL: https://v1-api.example.com
   V2 Base URL: https://v2-api.example.com

📋 Loaded 8 test cases

[1/8] Property: Property 97936 (OID=97936)
   Query: Default: Today+1, 2 Adults
   ✅ No differences found

[2/8] Property: Property 97936 (OID=97936)
   Query: 1 Night - 2 Adults
   ✅ No differences found

[3/8] Property: Property 97936 (OID=97936)
   Query: 2 Nights - Family
   ⚠️ Found 3 differences

...

📦 Generated 8 individual artifact files
📦 Summary artifact: artifacts/integration-test-summary_20251020_074500.json

=== Summary ===
Total test cases: 8
✅ Identical: 7
⚠️ With differences: 1
❌ Errors: 0

Test failed: 1 test case(s) have differences between V1 and V2.
Check artifacts folder for details: artifacts/
```

## Cross-Platform Support

✅ Fully compatible with:
- Windows
- Linux
- macOS

All path operations use cross-platform .NET APIs.

## Troubleshooting

### Tests are skipped
**Solution:** Set `IntegrationTest:Enabled` to `true`

### Connection errors
**Solution:** 
- Verify base URLs are correct
- Ensure APIs are accessible
- Check network connectivity

### No artifacts generated
**Solution:** 
- Check write permissions on artifacts folder
- Verify folder path in configuration

### Test timeout
**Solution:** Increase timeout in `AvailabilityIntegrationTests.cs`:
```csharp
_httpClient = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(10) // Increase from 5 to 10
};
```

## Project Structure

```
TOCC.IBE.Compare/
├── TOCC.IBE.Compare/              # Core comparison engine
│   └── AvailabilityComparer.cs    # Deep comparison logic
├── TOCC.IBE.Compare.Models/       # Data models
│   ├── Common/                    # Shared models
│   ├── Core/                      # Core types
│   └── V1/                        # V1 API models
└── TOCC.IBE.Compare.Tests/        # Test project
    ├── IntegrationTests/          # Integration test suite
    ├── Models/                    # Test data models
    ├── TestData/                  # Test configuration
    │   └── test-cases.json        # Test cases definition
    ├── appsettings.json           # Configuration
    └── artifacts/                 # Generated test results
```

## Contributing

When adding new test scenarios:

1. Add query configurations to `test-cases.json`
2. Run tests locally to verify
3. Check artifacts for any unexpected differences
4. Update skip paths if needed for known differences

## License

Internal tool for TOCC IBE API testing.
