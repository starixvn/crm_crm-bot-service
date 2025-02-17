using CRM.BotService.Core.Extensions;
using CRM.BotService.Core.Models;
using CRM.BotService.Core.Services;
using CRM.BotService.Endpoint;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Logging.Sentry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NLog.Web;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Text;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost
    .UseUrls("http://*:5000")
    .UseNLog();

ConfigureServices(builder.Services, builder.Configuration);
RegisterJwtConfiguration(builder.Services, builder.Configuration);
ConfigDataProtection(builder.Services, builder.Configuration);
ConfigRedisCache(builder.Services, builder.Configuration);
ConfigureLogging(builder.Configuration);

builder.Services.RegisterBotServices(builder.Configuration);

var app = builder.Build();
ConfigureMiddleware(app);
app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddFanexData(configuration);
    services.AddControllers().AddNewtonsoftJson();
    services.AddHttpClient();

    services.AddSingleton<IDynamicRepository, DynamicRepository>();
    services.AddSingleton<IBaseService, BaseService>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddSingleton<ILogger, Logger>();
    services.AddSingleton<ILogServiceExtend, LogServiceExtend>();
    services.AddSingleton<ILetstalkSendMultipleReceiverService, LetstalkSendMultipleReceiverService>();
}

void ConfigureLogging(IConfiguration configuration)
{
    LogManager
        .SetDefaultLogCategory(configuration.GetSection("LogCategory").Value)
        .Use(new List<ILogEngine>
        {
            new SentryLogging(new SentryEngineOptions
            {
                Dsn = configuration.GetSection("SentryEngineOptions:Dsn").Value,
            })
        });
}

void RegisterJwtConfiguration(IServiceCollection services, IConfiguration configuration)
{
    var appSettingsSection = configuration.GetSection("WebSettings");
    services.Configure<WebSettings>(appSettingsSection);
    var appSettings = appSettingsSection.Get<WebSettings>();
    services.AddSingleton(appSettings);

    var key = Encoding.ASCII.GetBytes(appSettings.Secret);
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
}

void ConfigDataProtection(IServiceCollection services, IConfiguration configuration)
{
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration.GetSection("Fanex.Caching.Redis:ConnectionString").Value;
        options.InstanceName = configuration.GetSection("Fanex.Caching.Redis:ApplicationName").Value + ":Session";
    });

    string aplicationName = configuration.GetSection("Fanex.Caching.Redis:ApplicationName").Value;
    services.AddDataProtection()
    .SetApplicationName(aplicationName)
        .PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(configuration.GetSection("Fanex.Caching.Redis:ConnectionString").Value),
        $"CRM.BotServiceEndpoint:DataProtection:Keys");
}

void ConfigRedisCache(IServiceCollection services, IConfiguration configuration)
{
    services.AddRedisCache(configuration.GetSection("Fanex.Caching.Redis"));
    const short WorkerThreads = 300;
    const short CompletionPortThreads = 300;
    _ = ThreadPool.SetMinThreads(WorkerThreads, CompletionPortThreads);
}
void ConfigureMiddleware(WebApplication app)
{
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
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{id?}");
}