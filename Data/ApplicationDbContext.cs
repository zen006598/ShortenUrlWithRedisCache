using Microsoft.EntityFrameworkCore;

namespace ShortenUrlWithRedis.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    { }
}