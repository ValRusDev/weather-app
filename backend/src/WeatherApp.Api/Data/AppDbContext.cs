using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Models;

namespace WeatherApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<WeatherRequestLog> WeatherRequestLogs => Set<WeatherRequestLog>();
}