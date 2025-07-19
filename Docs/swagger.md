## API Documentation with Swagger (Swashbuckle)

This project uses **Swashbuckle** to generate interactive API documentation.

### 🔍 Purpose

Swagger UI provides a visual interface for:
- Exploring available endpoints
- Testing requests and responses
- Auto-generating and hosting OpenAPI (Swagger) specs

### 🧪 Running Locally

```bash
dotnet run
open http://localhost:5190/swagger


### Problem during setup
 Two Swagger stacks mixed: AddOpenApi (the minimal‑API helper) generated /openapi/v1.json.
Swashbuckle’s UI middleware ran earlier and reset its endpoint list to the default /swagger/v1/swagger.json. UI couldn’t find JSON:Browser asked for /swagger/v1/swagger.json; server only had /openapi/v1.json → 404.
### Fix
removed the minimal‑API helper and used Swashbuckle end‑to‑end (AddEndpointsApiExplorer, AddSwaggerGen, UseSwagger, UseSwaggerUI). Now the UI and JSON live at the same path, so everything loads
### Conclusion
Swashbuckle is the source of truth; no AddOpenApi()