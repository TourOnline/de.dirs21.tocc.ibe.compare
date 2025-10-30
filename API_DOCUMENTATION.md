# TOCC IBE Compare API Documentation

## Overview

The TOCC IBE Compare API allows you to compare responses between V1 and V2 availability APIs to identify differences and ensure compatibility during migration. This service helps validate that both API versions return equivalent data for the same requests.

> Terminology:
> - **V1** = Current live/production instance
> - **V2** = RC (release candidate) instance

## Base URL

```
http://172.16.0.14:30052/api/comparison
```

## Main Comparison Endpoint

### Execute Comparison

**Endpoint:** `POST /api/comparison/execute`

**Description:** Compares V1 and V2 API responses for specified properties and test cases.

**Content-Type:** `application/json`

---

## Request Structure

The API accepts two modes of operation based on the `UsePreDefinedTestCases` flag:

### Mode 1: Using Pre-Defined Test Cases (`"UsePreDefinedTestCases": "true"`)

When set to `"true"`, the system uses test cases from the `TestData/test-cases.json` file.

#### Available Pre-Defined Test Cases:

1. **"1 Night - 2 Adults"**
   - Length of Stay: 1 night
   - Occupancy: `["a,a"]` (2 adults in 1 room)
   - Build Level: Primary

2. **"2 Nights - 2 Adults + 1 Child"**
   - Length of Stay: 2 nights  
   - Occupancy: `["a,a,5"]` (2 adults + 1 child age 5 in 1 room)
   - Build Level: Primary

3. **"1 Week - 4 Adults (2 Rooms)"**
   - Length of Stay: 7 nights
   - Occupancy: `["a,a", "a,a"]` (2 adults per room, 2 rooms total)
   - Build Level: Primary

> **Note**:
> - The generator always prepends a built-in default test case created in code: **Today + 1 day, 2 adults, 1 night** (labelled e.g. "Default: Today+1, 2 Adults").
> - For the configured scenarios listed above, the generator picks a random future `From` date; `Until` is calculated as `From + Los`.
> - You can add more pre-defined scenarios by editing `TestData/test-cases.json` (add entries under `QueryConfigurations`).

#### Request Body Example:

```json
{
  "Properties": [
    {
      "_oid": "98608",
      "_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8",
      "Description": "Mosers Blume Hotel",
      "UsePreDefinedTestCases": "true"
    }
  ]
}
```

#### Postman cURL:

```bash
curl --location 'http://172.16.0.14:30052/api/comparison/execute?explain=true' \
--header 'Content-Type: application/json' \
--data '{
  "Properties": [
    {
      "_oid": "98608",
      "_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8",
      "Description": "Mosers Blume Hotel",
      "UsePreDefinedTestCases": "true"
    }
  ]
}'
```

### Mode 2: Custom Test Cases (`"UsePreDefinedTestCases": "false"`)

When set to `"false"`, you must provide custom test cases in the request.

#### Request Body Example:

```json
{
  "Properties": [
    {
      "_oid": "98608",
      "_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8",
      "Description": "Mosers Blume Hotel",
      "UsePreDefinedTestCases": "false",
      "TestCases": [
        {
          "Occupancy": ["a,a"],
          "BuildLevel": "Primary",
          "From": "2025-12-03",
          "Until": "2025-12-04",
          "Los": 1,
          "OutputMode": "Availability",
          "Channel_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8"
        },
        {
          "Occupancy": ["a,a,5"],
          "BuildLevel": "Primary", 
          "From": "2025-12-05",
          "Until": "2025-12-07",
          "Los": 2,
          "OutputMode": "Availability",
          "Channel_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8"
        }
      ]
    }
  ]
}
```

#### Postman cURL:

```bash
curl --location 'http://172.16.0.14:30052/api/comparison/execute?explain=false' \
--header 'Content-Type: application/json' \
--data '{
  "Properties": [
    {
      "_oid": "98608",
      "_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8",
      "Description": "Mosers Blume Hotel",
      "UsePreDefinedTestCases": "false",
      "TestCases": [
        {
          "Occupancy": ["a,a"],
          "BuildLevel": "Primary",
          "From": "2025-12-03",
          "Until": "2025-12-04",
          "Los": 1,
          "OutputMode": "Availability",
          "Channel_uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8"
        }
      ]
    }
  ]
}'
```

---

## Request Fields Reference

### PropertyTestCase Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `_oid` | string | ✅ | Property OID identifier |
| `_uuid` | string | ✅ | Property UUID identifier |
| `Description` | string | ❌ | Human-readable property description |
| `UsePreDefinedTestCases` | string | ✅ | `"true"` or `"false"` |
| `TestCases` | array | ⚠️ | Required when `UsePreDefinedTestCases` is `"false"` |

### TestCaseParameters Fields

| Field | Type | Required | Description | Example |
|-------|------|----------|-------------|---------|
| `Occupancy` | string[] | ✅ | Room occupancy (adults/children) | `["a,a"]` or `["a,a,5"]` |
| `BuildLevel` | string | ✅ | API build level | `"Primary"` |
| `From` | string | ✅ | Start date (yyyy-MM-dd) | `"2025-12-03"` |
| `Until` | string | ✅ | End date (yyyy-MM-dd) | `"2025-12-04"` |
| `Los` | integer | ✅ | Length of stay (days) | `1` |
| `OutputMode` | string | ✅ | Response format | `"Availability"` |
| `Channel_uuid` | string | ✅ | Channel identifier | `"uuid-here"` |
 

---

## Response Structure

### Success Response (200 OK)

```json
{
  "timestamp": "2025-10-29T22:10:00.000Z",
  "totalTestCases": 2,
  "successfulComparisons": 1,
  "failedComparisons": 1,
  "totalDifferences": 5,
  "results": [
    {
      "oid": "98608",
      "uuid": "0c0278ae-edb3-464b-88a5-674007a30ea8",
      "description": "Mosers Blume Hotel",
      "testCaseName": "Default: Today+1, 2 Adults",
      "success": false,
      "differenceCount": 5,
      "v1ExecutionTimeMs": 1250,
      "v2ExecutionTimeMs": 980,
      "differences": [
        {
          "type": "ValueMismatch",
          "path": "Result.Properties[_oid=98608].Periods[From=03.12.2025 00:00:00].Sets[Key=0|2:a].Products[_uuid=138b2c5f-f033-54f1-93f0-6323eaf44483].Ticks[From=03.12.2025 00:00:00].Offers[MainTariff_uuid=41e4c5ea-76fb-fef4-7f45-080c450ce5ed].Type",
          "expected": "Cheapest",
          "actual": "SameTariff",
          "explanation": "V1 classified this offer as 'Cheapest' while V2 classified it as 'SameTariff'. This indicates a difference in offer classification logic between API versions."
        }
      ]
    }
  ]
}
```

---

## How to Verify Results

### 1. Check Overall Success Rate

```json
{
  "totalTestCases": 10,
  "successfulComparisons": 8,
  "failedComparisons": 2
}
```

- **Success Rate**: `successfulComparisons / totalTestCases * 100`
- **80% success rate** in this example

### 2. Performance Comparison

Monitor execution times:
```json
{
  "v1ExecutionTimeMs": 1250,
  "v2ExecutionTimeMs": 980
}
```

- **V2 is faster** in this example (980ms vs 1250ms)
- Look for significant performance regressions

### 3. Path Analysis

Examine difference paths to identify patterns:
```
Result.Properties[_oid=98608].Periods[From=03.12.2025 00:00:00].Sets[Key=0|2:a].Products[_uuid=...].Ticks[From=03.12.2025 00:00:00].Offers[MainTariff_uuid=...].Type
```

This shows the exact location of differences in the response structure.

---

## Testing Strategies

### 1. Smoke Test (Pre-Defined Test Cases)
```bash
# Quick validation using existing test cases
curl -X POST "http://172.16.0.14:30052/api/comparison/execute" \
  -H "Content-Type: application/json" \
  -d '{"Properties":[{"_oid":"98608","_uuid":"0c0278ae-edb3-464b-88a5-674007a30ea8","UsePreDefinedTestCases":"true"}]}'
```

### 2. Regression Test (Custom Test Cases)
```bash
# Test specific scenarios with custom parameters
curl -X POST "http://172.16.0.14:30052/api/comparison/execute?explain=true" \
  -H "Content-Type: application/json" \
  -d '{"Properties":[{"_oid":"98608","_uuid":"0c0278ae-edb3-464b-88a5-674007a30ea8","UsePreDefinedTestCases":"false","TestCases":[{"Occupancy":["2"],"BuildLevel":"Primary","From":"2025-12-03","Until":"2025-12-04","Los":1,"OutputMode":"Availability","Channel_uuid":"0c0278ae-edb3-464b-88a5-674007a30ea8"}]}]}'
```

---

## Best Practices

### 1. Date Format
- Always use `yyyy-MM-dd` format for dates
- Ensure `Los` matches the difference between `From` and `Until` dates

### 2. Error Handling
- Check HTTP status codes first
- Parse error messages for validation issues

---

