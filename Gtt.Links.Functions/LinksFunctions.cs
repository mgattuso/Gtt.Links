using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtt.CodeWorks;
using Gtt.CodeWorks.Clients.HttpClient;
using Gtt.CodeWorks.Clients.HttpRequest;
using Gtt.CodeWorks.Functions.Host;
using Gtt.CodeWorks.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Gtt.Links.Functions
{
    public class LinksFunctions : ServiceCallFunctionBase
    {
        private readonly HttpRequestMessageRunner _runner;
        private readonly IServiceResolver _serviceResolver;
        private readonly TelemetryClient _telemetryClient;

        public LinksFunctions(
            HttpRequestMessageRunner runner,
            IServiceResolver serviceResolver,
            IHttpDataSerializer httpDataSerializer,
            ISerializationSchema serializationSchema,
            IChainedServiceResolver chainedServiceResolver,
            IStateDiagram stateDiagram,
            TelemetryClient telemetryClient) : base(runner, serviceResolver, httpDataSerializer, serializationSchema, chainedServiceResolver, stateDiagram, telemetryClient)
        {
            _runner = runner;
            _serviceResolver = serviceResolver;
            _telemetryClient = telemetryClient;
        }

        [FunctionName("ServiceCallFunction")]
        public Task<HttpResponseMessage> Call(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "{action}/{*service}")]
            HttpRequestMessage request,
            string action,
            string service,
            CancellationToken cancellationToken)
        {
            return Execute(request, action, service, cancellationToken);
        }

        [FunctionName("ServiceErrorsFunction")]
        public Task<HttpResponseMessage> Errors(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "errors/{*service}")]
            HttpRequestMessage request,
            string service,
            CancellationToken cancellationToken)
        {
            var dict = new Dictionary<string, string> { ["Service"] = service };
            var serviceInstance = _serviceResolver.GetInstanceByName(service);



            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        [FunctionName("Ping")]
        public Task<HttpResponseMessage> Ping(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ping")]
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var r = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    timestamp = DateTimeOffset.UtcNow
                }))
            };

            return Task.FromResult(r);
        }

        [FunctionName("ListFunction")]
        public Task<HttpResponseMessage> List(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "list")]
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var serviceInstance = _serviceResolver.GetRegistered();

            var t = serviceInstance.First().ResponseType.GetGenericArguments();

            var directory = new
            {
                Name = "Service Directory",
                Services = serviceInstance.Select(svc => new
                {
                    Name = svc.Name,
                    Request = new
                    {
                        Type = svc.RequestType.Name,
                        //Example = Activator.CreateInstance(svc.RequestType)
                    },
                    Response = new
                    {
                        Type = svc.ResponseType.GetGenericArguments()[0].Name,
                        //Example = Activator.CreateInstance(svc.ResponseType, args: new object[] { Activator.CreateInstance(svc.ResponseType.GetGenericArguments()[0]), new ResponseMetaData(svc, ServiceResult.Successful) })
                    },
                    Method = "POST",
                    Url = $"{Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}/api/call/{svc.Name}"
                })
            };

            var responseData = JsonConvert.SerializeObject(directory, jsonSettings);

            var res = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseData, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(res);
        }
    }
}
