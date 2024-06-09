using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ShortenUrlWithRedis.Data;
using ShortenUrlWithRedis.Entity;
using ShortenUrlWithRedis.Extension;
using ShortenUrlWithRedis.Model;
using ShortenUrlWithRedis.Service;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var conStrBuilder = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("DefaultConnection"))
{
    Password = builder.Configuration["MssqlConnection:Password"]
};
var mssqlConnection = conStrBuilder.ConnectionString;

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(mssqlConnection));

builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisConnection = options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.Configuration = redisConnection;
});

builder.Services.AddScoped<UrlShorteningService>();

builder.Services.ApplyCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.ApplyMigrateDatabase();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.MapPost("/api/shorten", async (
    ShortenUrlRequest request,
    UrlShorteningService urlShorteningService,
    ApplicationDbContext _dbContext,
    HttpContext httpContext,
    IDistributedCache cache) =>
{
    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var _))
    {
        return Results.BadRequest("Invalid URL");
    }

    var code = await urlShorteningService.GenerateUniqueCode();

    var shortenUrl = new ShortenedUrl
    {
        OriginalUrl = request.Url,
        ShortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/{code}",
        Code = code,
        CreatedAt = DateTime.Now
    };
    _dbContext.ShortenedUrls.Add(shortenUrl);
    await _dbContext.SaveChangesAsync();

    var cacheKey = $"shorturl:{code}";
    await cache.SetStringAsync(cacheKey, shortenUrl.ShortUrl, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    });

    return Results.Ok(shortenUrl.ShortUrl);
});


app.MapGet("/api/{code}", async (
    string code,
    UrlShorteningService urlShorteningService,
    ApplicationDbContext _dbContext,
    IDistributedCache cache,
    ILogger<Program> logger) =>
{
    var cacheKey = $"shorturl:{code}";
    var cachedUrl = await cache.GetStringAsync(cacheKey);
    if (cachedUrl != null)
    {
        return Results.Ok(cachedUrl);
    }

    var url = await urlShorteningService.GetOriginalUrl(code);
    if (url is null)
        return Results.NotFound();

    await cache.SetStringAsync(cacheKey, url, new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
    });

    return Results.Ok(url);
});


app.Run();
