using Microsoft.EntityFrameworkCore;
using ShortenUrlWithRedis.Data;

namespace ShortenUrlWithRedis.Service;

public class UrlShorteningService(ApplicationDbContext dbContext, ILogger<UrlShorteningService> logger)
{
    private readonly Random _random = new();
    public const int CodeLength = 7;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<UrlShorteningService> _logger = logger;

    public async Task<string> GenerateUniqueCode()
    {
        var codeChars = new char[CodeLength];

        while (true)
        {
            for (var i = 0; i < CodeLength; i++)
            {
                var randomIndex = _random.Next(Alphabet.Length - 1);

                codeChars[i] = Alphabet[randomIndex];
            }

            var code = new string(codeChars);

            if (!await _dbContext.ShortenedUrls.AnyAsync(s => s.Code == code))
                return code;
        }
    }

    public async Task<string> GetOriginalUrl(string code)
    {
        var shortenedUrl = await _dbContext.ShortenedUrls.FirstOrDefaultAsync(s => s.Code == code);
        _logger.LogInformation($"Original URL: {shortenedUrl?.OriginalUrl}");
        return shortenedUrl?.OriginalUrl;
    }
}
