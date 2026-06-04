namespace WebApiDemo.Models
{
    public record PanchangResponseDto(
        string Date,
        LocationDto Location,
        string Sunrise,
        string Weekday,
        string Rutu,
        string YearName,
        TithiDto Tithi,
     string SankalpaTemplate

    );

    public record PanchangResult(
    int TithiNumber,
    string TithiName,
    string Paksha,
    int NakshatraNumber,
    string NakshatraName,
    string Yoga,
    string Karana);
}
