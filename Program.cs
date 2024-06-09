using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
    HttpContext httpContext) =>
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

    return Results.Ok(shortenUrl.ShortUrl);
});


app.MapGet("/api/{code}", async (
    string code,
    UrlShorteningService urlShorteningService,
    ApplicationDbContext _dbContext) =>
{
    var url = await urlShorteningService.GetOriginalUrl(code);
    if (url is null)
        return Results.NotFound();

    return Results.Ok(url);
});


app.Run();
