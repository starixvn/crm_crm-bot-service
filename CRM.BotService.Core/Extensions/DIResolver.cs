using Fanex.Caching;

namespace CRM.BotService.Core.Extensions
{
    using Constants;
    using CRM.BotService.Core.Models.Letstalk.BotConfig;
    using EventHandler;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Models;
    using Services;
    using Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;

    public static class DIResolver
    {
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
}