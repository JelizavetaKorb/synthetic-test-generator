# SyntheticTestGenerator

## Description

SyntheticTestGenerator is a specification-based tool for automatically generating synthetic test cases targeting automotive ECU (Electronic Control Unit) software interfaces. Given formal or semi-formal interface specifications — such as AUTOSAR Classic/Adaptive service interfaces, FIBEX signal definitions, or custom signal-range profiles — the system derives boundary-value, equivalence-partition, and combinatorial test vectors, enabling thorough regression and conformance testing of ECU software without manually authored test scripts.

## Tech Stack

| Layer       | Technology                                      |
|-------------|------------------------------------------------|
| API         | ASP.NET Core 8 Web API (controllers)            |
| Core logic  | .NET 8 class library (no ASP.NET deps)          |
| Tests       | xUnit + FluentAssertions                        |
| API docs    | Swashbuckle / Swagger UI                        |
| Frontend    | Static HTML/JS (served by the API via wwwroot/) |


## How to Access

| URL        | What you get                                                                     |
|------------|----------------------------------------------------------------------------------|
| `/`        | Frontend UI (served from `wwwroot/` once frontend is built and copied in)        |
| `/swagger` | Interactive Swagger API documentation                                            |

> In local development the frontend is served separately (e.g. `npm run dev`). The CORS policy in `Development` mode allows any origin so the two servers can communicate on different ports.

---

> **Bachelor's thesis project** — Jelizaveta Korb
