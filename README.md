# Synthetic Test Generator

A web tool for generating synthetic test scenarios for automotive ECU software. Configure parameters, define sweeps, and download ready-to-use JSON test vectors.

**Live app:** [synthetic-test-generator.vercel.app](https://synthetic-test-generator.vercel.app)

---

## Stack

| Layer    | Technology |
|----------|-----------|
| Frontend | Vanilla JS, static HTML/CSS, deployed on Vercel |
| API      | ASP.NET Core 8, deployed on Render |
| Core     | .NET 8 class library (generation, validation, formatting) |
| Tests    | xUnit + FluentAssertions + JsonSchema.Net |

## Structure

```
frontend/   Static UI (no build step)
backend/
  TestGen.Api/    REST API (ASP.NET Core controllers)
  TestGen.Core/   Generation and formatting logic
  TestGen.Tests/  Unit + schema compliance tests
```

> Bachelor's thesis project by Jelizaveta Korb
