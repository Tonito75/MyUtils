using Common.Logger;
using Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .Configure<FreeBoxClientOptions>(builder.Configuration.GetSection("Settings"))
    .AddSingleton<IFreeBoxClient, FreeBoxClient>()
    .AddSingleton<ILogService, WorkerLogService>();

var app = builder.Build();

app.UseAuthorization();

app.MapControllers();

app.Run();
