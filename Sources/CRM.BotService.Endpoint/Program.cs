using CRM.BotService.Endpoint;
using Fanex.Data;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
builder.Host.UseNLog();

var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers().AddNewtonsoftJson();
services.AddHttpClient();
services.AddHealthChecks();
services.AddFanexData(configuration);
services.AddFanexLogSentry(configuration);
services.AddDataProtection()
    .SetApplicationName(configuration.GetSection("Fanex.Caching.Redis:ApplicationName").Value ?? string.Empty)
    .PersistKeysToStackExchangeRedis(
        await ConnectionMultiplexer.ConnectAsync(configuration.GetSection("Fanex.Caching.Redis:ConnectionString").Value ?? string.Empty),
        "CRM.BotServiceEndpoint:DataProtection:Keys");

services.RegisterServices();
services.RegisterJwtConfiguration(configuration);
services.ConfigRedisCache(configuration);
services.RegisterBotServices(configuration);
services.RegisterHttpProxyClient(configuration);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseMiddleware<ErrorHandler>();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    "default",
    "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();