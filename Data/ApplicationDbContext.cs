using Microsoft.EntityFrameworkCore;
using ShortenUrlWithRedis.Model;
using ShortenUrlWithRedis.Service;

namespace ShortenUrlWithRedis.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }
    public DbSet<ShortenedUrl> ShortenedUrls { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShortenedUrl>(builder =>
        {
            builder
                .Property(s => s.Code)
                .HasMaxLength(UrlShorteningService.CodeLength);

            builder
                .HasIndex(s => s.Code)
                .IsUnique();
        });
    }
}