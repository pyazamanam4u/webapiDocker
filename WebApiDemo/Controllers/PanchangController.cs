using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/panchang")]
public class PanchangController : ControllerBase
{
    private readonly IPanchangService _service;

    public PanchangController(
        IPanchangService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult Get(
        DateOnly date,
        double latitude,
        double longitude)
    {
        var sunrise =
            new DateTimeOffset(
                date.Year,
                date.Month,
                date.Day,
                6,
                0,
                0,
                TimeSpan.FromHours(5.5));

        var result =
            _service.Calculate(
                sunrise,
                latitude,
                longitude);

        return Ok(result);
    }
}