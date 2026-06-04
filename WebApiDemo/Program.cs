using WebApiDemo.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<RequestRateLimiter>();
builder.Services.AddSingleton<
    ISwissEphemerisService,
    SwissEphemerisService>();

builder.Services.AddScoped<
    IPanchangService,
    PanchangService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200",
                    "https://yourdomain.com")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Telugu Panchang API v1");
    c.RoutePrefix = "swagger";
});
app.UseCors("Angular");
app.UseAuthorization();

app.MapControllers();

app.Run();