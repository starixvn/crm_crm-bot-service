using CRM.BotService.Constants;
using CRM.BotService.EventHandler;
using CRM.BotService.Models;
using CRM.BotService.Models.Letstalk.BotConfig;
using CRM.BotService.Services;
using CRM.BotService.Services.Interfaces;
using Fanex.Caching;
using Fanex.Caching.Redis;
using Fanex.Data.Repository;
using Fanex.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace CRM.BotService.Endpoint;

public static class ServiceCollectionExtension
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<IDynamicRepository, DynamicRepository>();
        services.AddSingleton<IBaseService, BaseService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ILogger, Logger>();
        services.AddSingleton<ILogServiceExtend, LogServiceExtend>();
        services.AddSingleton<ILetstalkSendMultipleReceiverService, LetstalkSendMultipleReceiverService>();
    }

    public static void RegisterJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
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
                x.TokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
    }

    public static void ConfigRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRedisCache(configuration.GetSection("Fanex.Caching.Redis"));
        const short workerThreads = 300;
        const short completionPortThreads = 300;
        _ = ThreadPool.SetMinThreads(workerThreads, completionPortThreads);
    }

    public static void RegisterBotServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILetstalkEventHandler, LetstalkEventHandler>();
        services.AddSingleton<IBotCommonService, BotCommonService>();

        RegisterBotTelegramService(services, configuration);
        ResolveLetstalkServices(services, configuration);
    }

    private static void ResolveLetstalkServices(this IServiceCollection services, IConfiguration configuration)
    {
        var botConfigs = configuration.GetSection("LetstalkBotConfigurations").Get<List<LetstalkBotConfig>>();
        var botChannelInfo = configuration.GetSection("BotChannelInfo").Get<List<BotChannelInfo>>();
        var httpClientSettings = configuration.GetSection("HttpClientSetting").Get<HttpClientSetting>();

        services.AddSingleton(httpClientSettings);
        services.AddSingleton(botConfigs);
        services.AddSingleton(botChannelInfo);

        services.AddHttpClient(NamedHttpClients.ProxiedClient).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            UseProxy = httpClientSettings.IsUseProxy,
            Proxy = new WebProxy(httpClientSettings.ProxyAddress, httpClientSettings.BypassOnLocal)
        });

        services.AddSingleton<Func<long, ILetstalkClientService>>(serviceProvider => botId =>
        {
            var botConfig = botConfigs.Find(config => config.BotAccountId == botId);

            return botConfig is null
                ? throw new ArgumentNullException($"The letstalk bot ID: {botId} does not existed in config file")
                : (ILetstalkClientService)new LetstalkClientService(
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    serviceProvider.GetRequiredService<ICacheService>(),
                    botConfig);
        });
    }

    private static void RegisterBotTelegramService(IServiceCollection services, IConfiguration configuration)
    {
        var telegramWebhook = configuration.GetSection("WebhookConfigurations").Get<List<WebhookConfiguration>>();
        services.AddSingleton(telegramWebhook);

        var telegramBots = configuration.GetSection("TelegramBotConfigurations").Get<List<TelegramBotConfigurations>>();
        services.AddSingleton(telegramBots);

        var httpClientSettings = configuration.GetSection("HttpClientSetting").Get<HttpClientSetting>();
        services.AddSingleton(httpClientSettings);

        services.AddHttpClient(NamedHttpClients.ProxiedClient).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            UseProxy = httpClientSettings.IsUseProxy,
            Proxy = new WebProxy(httpClientSettings.ProxyAddress, httpClientSettings.BypassOnLocal)
        });

        services.AddSingleton<Func<long, ITelegramBotService>>(serviceProvider => botId =>
        {
            var botConfig = telegramBots.Find(config => config.BotId == botId);

            return botConfig is null
                ? throw new ArgumentNullException($"The telegram bot ID: {botId} does not existed in config file")
                : (ITelegramBotService)new TelegramBotService(
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    telegramBots,
                    serviceProvider.GetRequiredService<ILogServiceExtend>(),
                    serviceProvider.GetRequiredService<IBotCommonService>());
        });
    }
}