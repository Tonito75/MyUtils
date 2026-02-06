using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/{location}", async (string location, IConfiguration config) =>
{
    var lat = config[$"Locations:{location}:lat"];
    var lng = config[$"Locations:{location}:lng"];

    if(string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lng))
    {
        return Results.NotFound();
    }

    var meteoApiUrl = config["OpenMeteoApiUrl"];
    if (string.IsNullOrEmpty(meteoApiUrl))
    {
        return Results.InternalServerError("Api url was empty in configuration");
    }

    var rainWeatherCodes = config["RainingCodes"];
    if (string.IsNullOrEmpty(rainWeatherCodes))
    {
        return Results.InternalServerError("Raining codes were empty in configuration");
    }

    var rainWeaterCodesList = rainWeatherCodes.Split(',').ToList().Select(c => Convert.ToInt32(c));

    try
    {
        using var _client = new HttpClient();
        var response = await _client.GetStringAsync(meteoApiUrl.Replace("LAT",lat).Replace("LNG", lng));
        using var json = JsonDocument.Parse(response);

        var currentWeather = json.RootElement.GetProperty("current");

        var weatherCode = currentWeather.GetProperty("weather_code").GetInt32();
        var isDay = currentWeather.GetProperty("is_day").GetInt16() == 1;

        return Results.Ok(new ApiResponse(isDay, rainWeaterCodesList.Any(c => c == weatherCode)));

    }
    catch(Exception ex)
    {
        return Results.InternalServerError($"An error occured whle getting meteo data : {ex.Message}");
    }
});


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

record ApiResponse(bool IsDay, bool IsRaining);