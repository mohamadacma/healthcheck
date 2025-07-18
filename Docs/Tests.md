# Integration Test Setup – HealthCheckApi

## Problem
Initially, integration tests failed with the following error:

> Services for database providers 'Npgsql.EntityFrameworkCore.PostgreSQL' and 'Microsoft.EntityFrameworkCore.InMemory' have been registered in the service provider. Only a single database provider can be registered in a service provider.
This happened because the main app (`Program.cs`) was registering PostgreSQL via `UseNpgsql`, and the test project was also trying to register `UseInMemoryDatabase`  leading to a conflict.

## Solution
To fix this:
1. **Set the environment to "Test"** inside the test project using:
   ```csharp
   builder.UseEnvironment("Test");

2. Removed all existing registrations of ItemsDbContext and its options in the test’s ConfigureServices hook:

   var toRemove = services.Where(d =>
       d.ServiceType == typeof(DbContextOptions<ItemsDbContext>) ||
       d.ServiceType == typeof(ItemsDbContext) ||
       (d.ServiceType.IsGenericType &&
        d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
   ).ToList();

   foreach (var d in toRemove)
       services.Remove(d);

3. Added a clean in-memory version:

   services.AddDbContext<ItemsDbContext>(options =>
       options.UseInMemoryDatabase(_dbName));

4. Added missing usings:

   using Microsoft.AspNetCore.Hosting;  // for UseEnvironment