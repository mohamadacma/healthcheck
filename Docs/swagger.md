Purpose: “API docs & testing via Swagger UI”
How to run locally:
```csharp
dotnet run
open http://localhost:5190/swagger

### Problem 
 Two Swagger stacks mixed: AddOpenApi (the minimal‑API helper) generated /openapi/v1.json.
Swashbuckle’s UI middleware ran earlier and reset its endpoint list to the default /swagger/v1/swagger.json. UI couldn’t find JSON:Browser asked for /swagger/v1/swagger.json; server only had /openapi/v1.json → 404.
### Fix
removed the minimal‑API helper and used Swashbuckle end‑to‑end (AddEndpointsApiExplorer, AddSwaggerGen, UseSwagger, UseSwaggerUI). Now the UI and JSON live at the same path, so everything loads
### Conclusion
Swashbuckle is the source of truth; no AddOpenApi()