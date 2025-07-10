
using Microsoft.EntityFrameworkCore;

namespace HealthCheckApi.Models;

public record Item(int Id, string Name, int Quantity, DateTime LastUpdated);
