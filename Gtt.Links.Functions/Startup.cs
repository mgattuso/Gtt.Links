using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtt.CodeWorks.DataAnnotations;
using Gtt.CodeWorks.Functions.Host;
using Gtt.CodeWorks.JWT;
using Gtt.CodeWorks.Security;
using Gtt.CodeWorks.Services;
using Gtt.Links.Core;
using Gtt.Links.Wiring;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Gtt.Links.Functions.Startup))]

namespace Gtt.Links.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddTransient<JwtServices, JwtServices>();
            builder.Services
                .AddTransient<ICollectionPropertyNamingStrategy, DottedNumberCollectionPropertyNamingStrategy>();
            builder.Services.AddTransient<IUserResolver>(cfg => cfg.GetService<JwtServices>());
            builder.Services.AddTransient<IAuthTokenGenerator>(cfg => cfg.GetService<JwtServices>());
            builder.Services.AddTransient<IUserResolverSecret>(cfg =>
                new UserResolverSecretBasic(Environment.GetEnvironmentVariable("JWT.Secret.0001")));

            builder.Services.AddTransient<IEncryptionService>(cfg => new EncryptionService(
                Environment.GetEnvironmentVariable("Security.aes.0001"),
                Environment.GetEnvironmentVariable("Security.hmac.0001")
            ));

            builder.Services.ConfigureCodeWorksAll<HealthCheckService>("Gtt.Links.Core");
            builder.Services.ConfigureStateMachines(Environment.GetEnvironmentVariable("StateRepository"));
            builder.Services.AddApplicationInsightsTelemetry();
        }
    }
}
