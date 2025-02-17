namespace CRM.BotService.Services.Telegram.Extensions
{
    using CRM.BotService.Services.Letstalk.Services;
    using CRM.BotService.Services.Letstalk.Services.Interfaces;
    using CRM.BotService.Services.Telegram.Services;
    using CRM.BotService.Services.Telegram.Services.Interfaces;
    using CRM.Letstalk.Core.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    public static class DIResolver
    {
        public static void ResolveTelegramBotServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ITelegramClientService, TelegramClientService>();
            services.AddSingleton<IHttpClientService, HttpClientService>();
            services.Configure<TelegramAppSettings>(options => configuration.GetSection("TelegramAppSettings").Bind(options));
        }
    }
}
