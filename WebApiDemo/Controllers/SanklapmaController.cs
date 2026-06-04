using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebApiDemo.Models;
using WebApiDemo.Services;

namespace WebApiDemo.Controllers
{
    [ApiController]
    [Route("sanklapma")]
    public class SanklapmaController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly RequestRateLimiter _rateLimiter;

        public SanklapmaController(IMemoryCache cache, RequestRateLimiter rateLimiter)
        {
            _cache = cache;
            _rateLimiter = rateLimiter;
        }

        [HttpGet("tithi")]
        public ActionResult<PanchangResponseDto> GetTithi([
            FromQuery(Name = "date")] string? date,
            [FromQuery] double? latitude,
            [FromQuery] double? longitude)
        {
            if (string.IsNullOrWhiteSpace(date))
                return BadRequest(new { error = "Missing required query parameter: date" });

            if (!DateOnly.TryParse(date, out var parsedDate))
                return BadRequest(new { error = "Invalid date format. Use YYYY-MM-DD" });

            if (!latitude.HasValue || !longitude.HasValue)
                return BadRequest(new { error = "Missing required query parameters: latitude and longitude" });

            // Client identification for rate limiting
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";


            // Compute cache key based on date+lat+lon (used by ban handling and caching)
            var key = $"panchang:{parsedDate:yyyy-MM-dd}:{latitude.Value:F5}:{longitude.Value:F5}";

            // Enterprise policy: when a client is banned, optionally serve a cached (stale) response
            const bool serveCachedWhileBanned = true; // make configurable in production

            // Check ban
            if (_rateLimiter.IsBanned(clientIp, out var remainingBan))
            {
                // If we have a cached response and policy allows, serve it with STALE indicators
                if (serveCachedWhileBanned && _cache.TryGetValue<PanchangResponseDto>(key, out var cachedWhileBanned))
                {
                    Response.Headers["Retry-After"] = ((int)remainingBan.TotalSeconds).ToString();
                    Response.Headers["X-RateLimit-Limit"] = "3";
                    Response.Headers["X-RateLimit-Remaining"] = "0";
                    Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(remainingBan).ToUnixTimeSeconds().ToString();
                    Response.Headers["X-Cache"] = "STALE";
                    // Include advisory header that client is rate-limited but served cached data
                    Response.Headers["X-RateLimit-Note"] = "Client is rate-limited; serving cached data until ban expires.";
                    return Ok(cachedWhileBanned);
                }

                Response.Headers["Retry-After"] = ((int)remainingBan.TotalSeconds).ToString();
                Response.Headers["X-RateLimit-Limit"] = "3";
                Response.Headers["X-RateLimit-Remaining"] = "0";
                Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.Add(remainingBan).ToUnixTimeSeconds().ToString();
                return StatusCode(StatusCodes.Status429TooManyRequests, new { error = "Too many requests", retryAfterSeconds = (int)remainingBan.TotalSeconds });
            }

            // Register this request for IP-based limiting
            _rateLimiter.RegisterRequestFromIp(clientIp);

            // Register access for key (may mark long cache)
            _rateLimiter.RegisterAccessForKey(key);

            // If cached, return cached response
            if (_cache.TryGetValue<PanchangResponseDto>(key, out var cached))
            {
                Response.Headers["X-Cache"] = "HIT";
                var remain = Math.Max(0, 3 - _rateLimiter.GetRequestsInLastHourForIp(clientIp));
                Response.Headers["X-RateLimit-Limit"] = "3";
                Response.Headers["X-RateLimit-Remaining"] = remain.ToString();
                Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString();
                return Ok(cached);
            }

            // Compute response dynamically (approximation)
            var istOffset = TimeSpan.FromHours(5.5);

            DateTimeOffset MakeDto(DateOnly d, int hour, int minute, int second)
                => new DateTimeOffset(d.Year, d.Month, d.Day, hour, minute, second, istOffset);

            // Use synodic-month-based approximation calibrated to a base instant
            const double synodicDays = 29.530588853;
            var baseInstantLocal = new DateTimeOffset(2026, 6, 2, 12, 0, 0, istOffset);
            var baseInstantUtc = baseInstantLocal.UtcDateTime;
            const int baseTithi = 2; // calibration

            var localNoon = new DateTimeOffset(parsedDate.Year, parsedDate.Month, parsedDate.Day, 12, 0, 0, istOffset);
            var utcInstant = localNoon.UtcDateTime;

            var daysSinceBase = (utcInstant - baseInstantUtc).TotalDays;
            var totalTithisSinceBase = daysSinceBase / synodicDays * 30.0;
            var deltaTithis = (int)Math.Round(totalTithisSinceBase);
            var tithiNumber = ((baseTithi - 1 + deltaTithis) % 30 + 30) % 30 + 1;

            var tithiIndexSinceBase = Math.Floor((baseTithi - 1) + totalTithisSinceBase);
            var tithiStartUtc = DateTime.SpecifyKind(baseInstantUtc.AddDays(tithiIndexSinceBase * (synodicDays / 30.0)), DateTimeKind.Utc);
            var tithiEndUtc = tithiStartUtc.AddDays(synodicDays / 30.0);
            var tithiStart = new DateTimeOffset(tithiStartUtc).ToOffset(istOffset);
            var tithiEnd = new DateTimeOffset(tithiEndUtc).ToOffset(istOffset);

            var names = TithiNames.GetNames();
            var englishName = names.English[(tithiNumber - 1) % names.English.Length];
            var teluguName = names.Telugu[(tithiNumber - 1) % names.Telugu.Length];

            var paksha = tithiNumber <= 15 ? "Shukla Paksha" : "Krishna Paksha";

            var sunrise = MakeDto(parsedDate, 6, 0, 0);
            var prevailingAtSunrise = sunrise >= tithiStart && sunrise <= tithiEnd;

            // Approximate nakshatra
            var nakshatraNames = new[] { "Ashwini", "Bharani", "Krittika", "Rohini", "Mrigashira", "Ardra", "Punarvasu", "Pushya", "Ashlesha", "Magha", "Purva Phalguni", "Uttara Phalguni", "Hasta", "Chitra", "Swati", "Vishakha", "Anuradha", "Jyeshta", "Mula", "Purva Ashadha", "Uttara Ashadha", "Shravana", "Dhanishta", "Shatabhisha", "Purva Bhadrapada", "Uttara Bhadrapada", "Revati" };
            var totalNakshatrasSinceBase = totalTithisSinceBase * (27.0 / 30.0);
            int nakIndex = ((int)Math.Floor(totalNakshatrasSinceBase) % 27 + 27) % 27;
            var nakshatra = nakshatraNames[nakIndex];

            var yoga = "";
            var karana = "";
            var moonRashi = "";
            var sankalpaTemplate = $"Om Namo Narayanaya. Today is {parsedDate:yyyy-MM-dd}, tithi {tithiNumber} ({englishName}).";

            string weekday = parsedDate.ToDateTime(TimeOnly.MinValue).ToString("dddd");
            string rutu = "Greeshma";

            // Year name simple mapping
            var samvatsaraNames = new[] { "Prabhava", "Vibhava", "Shukla", "Pramoda", "Prajotpatti", "Angirasa", "Shrimukha", "Bhava", "Yuva", "Dhatu", "Ishvara", "Bahudhanya", "Pramathi", "Vikrama", "Vrisha", "Chitrabhanu", "Subhanu", "Tara", "Parthiva", "Vyaya", "Sarvajit", "Sarvadhari", "Virodhi", "Vikriti", "Khara", "Nandana", "Vijaya", "Jaya", "Manmatha", "Durmukha", "Hevilambi", "Vilambi", "Vikala", "Sharvari", "Plava", "Shubhakruthu", "Shobhakruthu", "Krodhi", "Vishvavasu", "Parabhava", "Plavanga", "Kilaka", "Saumya", "Sadharana", "Virodhikruthu", "Paritapitha", "Pramadhisha", "Ananda", "Raksasa", "Nala", "Pingala", "Kalayukthi", "Siddharthi", "Raudra", "Durmathi", "Dundubhi", "Rudhirodgari", "Raktakshi", "Krodhana", "Akshaya" };
            int baseYear = 1987;
            int index = Math.Abs(parsedDate.Year - baseYear) % samvatsaraNames.Length;
            string yearName = samvatsaraNames[index];

            var response = new PanchangResponseDto(
                Date: parsedDate.ToString("yyyy-MM-dd"),
                Location: new LocationDto(latitude.Value, longitude.Value),
                Sunrise: sunrise.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                Weekday: weekday,
                Rutu: rutu,
                YearName: yearName,
                Tithi: new TithiDto(
                    Number: tithiNumber,
                    EnglishName: englishName,
                    TeluguName: teluguName,
                    Paksha: paksha,
                    StartTime: tithiStart.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    EndTime: tithiEnd.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    PrevailingAtSunrise: prevailingAtSunrise,
                    Nakshatra: nakshatra,
                    Yoga: yoga,
                    Karana: karana,
                    MoonRashi: moonRashi,
                    SankalpaTemplate: sankalpaTemplate
                )
            );

            // Cache response
            var cacheOptions = new MemoryCacheEntryOptions();
            if (_rateLimiter.IsLongCached(key))
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            }
            else
            {
                cacheOptions.SlidingExpiration = TimeSpan.FromMinutes(5);
            }
            _cache.Set(key, response, cacheOptions);

            Response.Headers["X-Cache"] = "MISS";
            var remain2 = Math.Max(0, 3 - _rateLimiter.GetRequestsInLastHourForIp(clientIp));
            Response.Headers["X-RateLimit-Limit"] = "3";
            Response.Headers["X-RateLimit-Remaining"] = remain2.ToString();
            Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString();

            return Ok(response);
        }

        [HttpGet("generateSankaplam")]
        public ActionResult<PanchangResponseDto> GenerateSankaplam() { 
            
            return Ok();
        
        
        }
    }
}
