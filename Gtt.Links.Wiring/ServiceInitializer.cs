using System;
using System.Collections.Generic;
using Gtt.CodeWorks;
using Microsoft.Extensions.DependencyInjection;

namespace Gtt.Links.Wiring
{
    public static class ServiceInitializer
    {
        public static IServiceProvider Create()
        {
            var collection = new ServiceCollection();

            collection.AddLogging();
            collection.ConfigureCoreDependencies();
            collection.ConfigureLogging();
            collection.ConfigureAuthentication();
            collection.ConfigureHttp();
            collection.ConfigureServices();
            collection.ConfigureStateMachines(Environment.GetEnvironmentVariable("StateRepository"));

            var provider = collection.BuildServiceProvider();
            return provider;
        }

        public static Tuple<IEnumerable<IServiceInstance>, IServiceResolver> GetInstances()
        {
            var sp = Create();
            var instances = sp.GetService<IEnumerable<IServiceInstance>>();
            var resolver = sp.GetService<IServiceResolver>();
            return Tuple.Create(instances, resolver);
        }
    }
}