namespace WebApiDemo.Models
{
public record TithiDto(
    int Number,
    string EnglishName,
    string TeluguName,
    string Paksha,
    string StartTime,
    string EndTime,
    bool PrevailingAtSunrise,
    string Nakshatra,
    string Yoga,
    string Karana,
    string MoonRashi
);
}