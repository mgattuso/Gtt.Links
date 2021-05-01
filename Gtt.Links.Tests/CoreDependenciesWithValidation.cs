using System;
using System.Collections.Generic;
using System.Text;
using Gtt.CodeWorks;
using Gtt.CodeWorks.DataAnnotations;
using Gtt.CodeWorks.Tokenizer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Gtt.Links.Tests
{
    public class CoreDependenciesWithValidation
    {
        public static CoreDependencies Instance
        {
            get
            {
                var lf = new NullLoggerFactory();
                return new CoreDependencies(
                    lf, NullServiceLogger.Instance,
                    new CodeWorksTokenizer(new NullTokenService()), 
                    NullRateLimiter.NoLimits,
                    NullDistributedLockService.NoLock,
                    new NonProductionEnvironmentResolver(),
                    new DataAnnotationsRequestValidator(new NullServiceProvider()),
                    new DefaultChainedServiceResolver(lf.CreateLogger<DefaultChainedServiceResolver>()),
                    NullUserResolver.Instance
                );
            }
        }

        public class NullServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }
    }
}
