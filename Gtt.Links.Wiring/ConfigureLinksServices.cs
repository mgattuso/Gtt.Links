using System;
using System.Collections.Generic;
using System.Linq;
using Gtt.CodeWorks;
using Gtt.CodeWorks.Clients.HttpRequest;
using Gtt.CodeWorks.DataAnnotations;
using Gtt.CodeWorks.JWT;
using Gtt.CodeWorks.Security;
using Gtt.CodeWorks.Serializers.TextJson;
using Gtt.CodeWorks.Services;
using Gtt.CodeWorks.StateMachines;
using Gtt.CodeWorks.StateMachines.AzureStorage;
using Gtt.CodeWorks.Validation;
using Gtt.CodeWorks.Tokenizer;
using Gtt.Links.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tokenize.Client;

namespace Gtt.Links.Wiring
{
    public static class ConfigureLinksServices
    {
        public static void ConfigureLogging(this IServiceCollection services)
        {
            services.AddTransient<ILogObjectSerializer, JsonLogObjectSerializer>();
            services.AddTransient<IServiceLogger, ServiceLogger>();
        }

        public static void ConfigureCoreDependencies(this IServiceCollection services)
        {
            services.AddTransient<ITokenizeClient>(cfg => new TokenizeClient(
                Environment.GetEnvironmentVariable("TokenizeEndpoint"),
                "codeworks",
                Environment.GetEnvironmentVariable("TokenizeKey")
                ));
            services.AddTransient<ITokenizerService, GttTokenizerService>();
            services.AddTransient<ICodeWorksTokenizer, CodeWorksTokenizer>();
            services.AddTransient<IRateLimiter>(cfg => new InMemoryRateLimiter());
            services.AddTransient<IDistributedLockService>(cfg => new InMemoryDistributedLock());
            services.AddTransient<IServiceEnvironmentResolver>(cfg => new NonProductionEnvironmentResolver());
            services.AddTransient<IRequestValidator, DataAnnotationsRequestValidator>();
            services.AddScoped<IChainedServiceResolver, DefaultChainedServiceResolver>();
            services.AddTransient<IEncryptionService>(cfg => new EncryptionService(
                Environment.GetEnvironmentVariable("Security.aes"),
                Environment.GetEnvironmentVariable("Security.hmac")
            ));
            services.AddTransient<CoreDependencies>();
        }

        public static void ConfigureHttp(this IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddTransient<IHttpDataSerializer, HttpJsonDataSerializer>();
            services.AddTransient<HttpRequestMessageConverter>();
            services.AddTransient<HttpRequestMessageRunner>();
        }

        public static void ConfigureAuthentication(this IServiceCollection services)
        {
            services.AddTransient<JwtServices, JwtServices>();
            services.AddTransient<IUserResolver>(cfg => cfg.GetService<JwtServices>());
            services.AddTransient<IAuthTokenGenerator>(cfg => cfg.GetService<JwtServices>());
            services.AddTransient<IUserResolverSecret>(cfg =>
                new UserResolverSecretBasic(Environment.GetEnvironmentVariable("JWT.Secret")));
        }

        public static void ConfigureServices(this IServiceCollection services)
        {
            foreach (var svc in GetConcreteInstancesOf<IServiceInstance>())
            {
                Console.WriteLine($"Registering Service {svc.Name}");
                services.AddScoped(svc);
                services.AddScoped(cfg => (IServiceInstance)cfg.GetService(svc));
            }
            services.AddSingleton<IServiceResolver>(cfg => new ServiceResolver(cfg.GetServices<IServiceInstance>(), new ServiceResolverOptions
            {
                NamespacePrefixToIgnore = "Gtt.Links.Core"
            }));
        }

        public static void ConfigureStateMachines(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<StatefulDependencies, StatefulDependencies>();
            services.AddTransient<IObjectSerializer>(cfg => new JsonObjectSerializer(false));
            services.AddTransient<IStateDiagram, StatelessStateDiagram>();
            services.AddTransient<IStateRepository>(cfg =>
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return new InMemoryStateRepository();
                }

                return new AzureTableStateRepository(
                    connectionString,
                    cfg.GetService<IObjectSerializer>(),
                    cfg.GetService<ILogger<AzureTableStateRepository>>()
                );
            });
        }

        private static IEnumerable<Type> GetConcreteInstancesOf<T>()
        {
            var a = typeof(HealthCheckService).Assembly;
            var cls = a.GetTypes().Where(p => typeof(T).IsAssignableFrom(p) && !p.IsAbstract);
            return cls;
        }

    }
}
