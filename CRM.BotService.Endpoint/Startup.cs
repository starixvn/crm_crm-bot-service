namespace CRM.BotService.Endpoint
{
    using Core.Extensions;
    using Core.Models;
    using Core.Services;
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
    using StackExchange.Redis;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;
    using System.Threading;

    [ExcludeFromCodeCoverage(Justification = "Configures application startup")]
    public class Startup
    {
        private readonly ConnectionMultiplexer Redis;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Redis = ConnectionMultiplexer.Connect(Configuration.GetSection("Fanex.Caching.Redis:ConnectionString").Value);
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddFanexData(Configuration);

            LogManager
            .SetDefaultLogCategory(Configuration.GetSection("LogCategory").Value)
            .Use(new List<ILogEngine>
            {
                new SentryLogging(new SentryEngineOptions
                {
                    Dsn = Configuration.GetSection("SentryEngineOptions:Dsn").Value,
                })
            });

            services.AddControllers().AddNewtonsoftJson();
            services.AddHttpClient();
            ConfigDataProtection(services);
            ConfigRedisCache(services);
            RegisterJwtConfiguration(services, Configuration);

            RegisterServices(services);
            services.RegisterBotServices(Configuration);
        }

        private static void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IDynamicRepository, DynamicRepository>();
            services.AddSingleton<IBaseService, BaseService>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<ILogServiceExtend, LogServiceExtend>();
            services.AddSingleton<ILetstalkSendMultipleReceiverService, LetstalkSendMultipleReceiverService>();
        }

        private static void RegisterJwtConfiguration(IServiceCollection services, IConfiguration configuration)
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

        private void ConfigDataProtection(IServiceCollection services)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration.GetSection("Fanex.Caching.Redis:ConnectionString").Value;
                options.InstanceName = Configuration.GetSection("Fanex.Caching.Redis:ApplicationName").Value + ":Session";
            });

            string aplicationName = Configuration.GetSection("Fanex.Caching.Redis:ApplicationName").Value;
            services.AddDataProtection()
                .SetApplicationName(aplicationName)
                .PersistKeysToStackExchangeRedis(Redis, $"CRM.BotServiceEndpoint:DataProtection:Keys");
        }

        private void ConfigRedisCache(IServiceCollection services)
        {
            services.AddRedisCache(Configuration.GetSection("Fanex.Caching.Redis"));
            const short WorkerThreads = 300;
            const short CompletionPortThreads = 300;
            _ = ThreadPool.SetMinThreads(WorkerThreads, CompletionPortThreads);
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Auth}/{action=Index}/{id?}");
            });
        }
    }
}