using PricingSystem.Services;
using Serilog;
using PricingSystem.Logging;
using PricingSystem.Interfaces;
using PricingSystem.Controllers;
using Grpc.Core;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
builder.Services.AddControllers();

builder.Services.AddGrpc();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = configurationBuilder.Build();

builder.Services.AddSingleton<IConfiguration>(configuration);
LogConfiguration.ConfigureSerilog(configuration);
builder.Services.AddLogging(configure => { configure.AddSerilog(); });

builder.Services.AddSingleton<IPricingService, PricingService>();

builder.Services.AddHttpClient("LiveMarketClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ThirdPartyPriceCheckURL"]);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

builder.Services.AddSingleton<LiveMarketDataCache>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("LiveMarketClient");

    var logger = sp.GetRequiredService<ILogger<LiveMarketDataCache>>();
    return new LiveMarketDataCache(logger, client);
});

builder.Services.AddSingleton<ILiveMarketDataCache>(sp => sp.GetRequiredService<LiveMarketDataCache>());


builder.Services.AddHostedService(p => p.GetRequiredService<IPricingService>());

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGrpcService<LiveMarketDataCache>();

app.MapEndpoints();

app.MapFallbackToFile("/index.html");

app.Run();
