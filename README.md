# Synthetic Test Generator

A web tool for generating synthetic test scenarios for automotive ECU software. Configure parameters, define sweeps, and download ready-to-use JSON test vectors.

**Live app:** [synthetic-test-generator.vercel.app](https://synthetic-test-generator.vercel.app)

---

## Stack

| Layer    | Technology |
|----------|-----------|
| Frontend | Vanilla JS, static HTML/CSS — deployed on Vercel |
| API      | ASP.NET Core 8 — deployed on Render |
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

## Running locally

```bash
# Backend (port 5000)
cd backend && dotnet run --project TestGen.Api

# Frontend — open frontend/index.html directly in a browser,
# or serve it with any static server:
cd frontend && npx serve .
```

Set `VITE_API_BASE` (or edit `frontend/env.js`) to point at the local backend if needed.

## Tests

```bash
cd backend && dotnet test
```

---

> Bachelor's thesis project — Jelizaveta Korb
