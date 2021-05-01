using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtt.CodeWorks;
using Gtt.CodeWorks.Duplicator;
using Gtt.CodeWorks.StateMachines;
using Gtt.Links.Core;
using Gtt.Links.Wiring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gtt.Links.CodeGen
{
    [Generator]
    public class ClientGenerator : ISourceGenerator
    {
        private IEnumerable<IServiceInstance> _instances;
        private IServiceResolver _resolver;
        private readonly string _clientName;

        public ClientGenerator()
        {
            _clientName = "Links";
        }

        public ClientGenerator(string coreLibraryName)
        {
            _clientName = coreLibraryName;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            var sp = ServiceInitializer.GetInstances();
            _instances = sp.Item1;
            _resolver = sp.Item2;
        }

        private string Contracts(string codeLibraryName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var instance in _instances)
            {
                sb.AppendLine($@"
Task<ServiceResponse<{instance.GetClientResponseType(codeLibraryName)}>> {instance.Name}({instance.GetClientRequestType(codeLibraryName)} request, CancellationToken cancellationToken);
");
            }
            return sb.ToString();
        }

        private string ServiceBaseContracts(string coreLibraryName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var instance in _instances)
            {
                sb.AppendLine($@"
Task<ServiceResponse<{instance.GetClientResponseType(coreLibraryName)}>> {instance.Name}(IServiceInstance callingService, {instance.GetClientRequestType(coreLibraryName)} request, CancellationToken cancellationToken);
");
            }
            return sb.ToString();
        }

        private string EasyContracts(string coreLibraryName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var instance in _instances)
            {
                sb.AppendLine($@"
Task<{instance.GetClientResponseType(coreLibraryName)}> {instance.Name}Easy({instance.GetClientRequestType(coreLibraryName)} request, CancellationToken cancellationToken);
");
            }
            return sb.ToString();
        }

        private string Methods(string coreLibraryName)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var instance in _instances)
            {
                string responseType = instance.GetClientResponseType(coreLibraryName);
                string requestType = instance.GetClientRequestType(coreLibraryName);
                string call = "Call";
                string callGenericArguments = $"{requestType}, {responseType}";

                //if (instance is IStatefulServiceInstance ssi)
                //{
                //      call = "StatefulCall";
                //      string stateType = ssi.GetStateType(coreLibraryName);
                //      string triggerType = ssi.GetTriggerType(coreLibraryName);
                //      string dataType = ssi.GetDataType(coreLibraryName);
                //      callGenericArguments = $"{requestType}, {responseType}, {stateType}, {triggerType}, {dataType}";
                //}

                var s = $@"
            public Task<ServiceResponse<{responseType}>> {instance.Name}({requestType} request, CancellationToken cancellationToken){{
                var url =""{_resolver.GetRegisteredNameFromFullName(instance.FullName)}"";
                _logger.LogInformation(""Calling {{url}} with correlation id {{correlationId}}"", url, request.CorrelationId);
                return {call}<{callGenericArguments}>(url, request, cancellationToken);
            }}

            public async Task<{responseType}> {instance.Name}Easy({requestType} request){{
                var url =""{_resolver.GetRegisteredNameFromFullName(instance.FullName)}"";
                _logger.LogInformation(""Calling {{url}} with correlation id {{correlationId}}"", url, request.CorrelationId);
                var response = await {call}<{callGenericArguments}>(url, request, CancellationToken.None);
                if (response.MetaData.Result.Outcome() == ResultOutcome.Successful) 
                {{
                    return response.Data;
                }}
                
                throw new ServiceCallException<ServiceResponse<{responseType}>>(response);
            }}

            public async Task<{responseType}> {instance.Name}Easy({requestType} request, CancellationToken cancellationToken){{
                var url =""{_resolver.GetRegisteredNameFromFullName(instance.FullName)}"";
                _logger.LogInformation(""Calling {{url}} with correlation id {{correlationId}}"", url, request.CorrelationId);
                var response = await {call}<{callGenericArguments}>(url, request, cancellationToken);
                if (response.MetaData.Result.Outcome() == ResultOutcome.Successful) 
                {{
                    return response.Data;
                }}
                
                throw new ServiceCallException<ServiceResponse<{responseType}>>(response);
            }}

            public Task<ServiceResponse<{responseType}>> {instance.Name}(IServiceInstance callingService, {requestType} request, CancellationToken cancellationToken){{
                request.CorrelationId = callingService.CorrelationId;
                request.SessionId = callingService.SessionId;
                return {instance.Name}(request, cancellationToken);
            }}
        ";
                sb.AppendLine(s);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var clientCode = CreateClient(_clientName);
            context.AddSource("clientGenerator", SourceText.From(clientCode, Encoding.UTF8));

            var models = ModelDuplicator();
            Console.WriteLine(models);
            context.AddSource("modelGenerator", SourceText.From(models, Encoding.UTF8));
        }

        private string CreateClient(string clientName)
        {
            var text = $@"
using System;
using Gtt.Links.Client;
using System.Threading;
using Gtt.CodeWorks;
using System.Threading.Tasks;
using System.Net.Http;
using Gtt.CodeWorks.Clients.HttpClient;
using Gtt.CodeWorks.StateMachines.Clients.HttpClient;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Gtt.CodeWorks.Serializers.TextJson;

namespace Gtt.Links.Client 
{{
    
    public interface I{clientName}Client
    {{
        {Contracts(clientName)}
    }}

    public interface I{clientName}ClientServices
    {{
        {ServiceBaseContracts(clientName)}
    }}

    public interface I{clientName}ClientEasy
    {{
        {EasyContracts(clientName)}
    }}
    
    public class {clientName}ClientEndpoint : CodeWorksClientEndpoint 
    {{
        public {clientName}ClientEndpoint(string rootUrl, Dictionary<string, string> urlMap = null) : base(rootUrl, urlMap)
        {{ 
            
        }}
    }}

    public class {clientName}ClientEasy : {clientName}Client, I{clientName}ClientEasy
    {{
        private static HttpClient InternalClient = new HttpClient();
        private static ILoggerFactory NullFactory = NullLoggerFactory.Instance;

        public {clientName}ClientEasy(
            string rootUrl, 
            HttpClient client = null, 
            ILoggerFactory loggerFactory = null,
            Dictionary<string, string> urlMap = null)
            : base(
                    new {clientName}ClientEndpoint(rootUrl, urlMap), 
                    (client ?? InternalClient),
                    new HttpJsonDataSerializer((loggerFactory??NullFactory).CreateLogger<HttpJsonDataSerializer>()),
                    new DefaultHttpSerializerOptionsResolver(),
                    (loggerFactory??NullFactory)
                )
        {{
            
        }}
    }}

    public class {clientName}Client : CodeWorksStatefulClientBase, I{clientName}Client, I{clientName}ClientServices
    {{
        private ILogger _logger;
        
        public {clientName}Client(
            {clientName}ClientEndpoint endpoint, 
            HttpClient httpClient,
            IHttpDataSerializer httpDataSerializer,
            IHttpSerializerOptionsResolver optionsResolver,
            ILoggerFactory loggerFactory) : base(endpoint, httpClient, httpDataSerializer, optionsResolver, loggerFactory)
        {{
            _logger = loggerFactory.CreateLogger<{clientName}Client>();
        }}

        {Methods(clientName)}
    }}
}}";
            return text;
        }

        public string ModelDuplicator()
        {
            (string, string) replaceWith = ($"Gtt.Links.Core", $"Gtt.Links.Client");
            var settings = new CopierSettings();
            settings.BaseTypesToRemove.Add(typeof(BaseServiceInstance<,>));
            settings.BaseTypesToRemove.Add(typeof(BaseStatefulServiceInstance<,,,,>));
            var mt = new Copier(settings)
            {
                AlwaysGetAndSet = true,
                ReplaceNamespace = replaceWith
            };
            mt.LimitOutputToAssemblyOfType(typeof(HealthCheckRequest));

            foreach (var instance in _instances)
            {
                mt.AddType(instance.RequestType);
                mt.AddType(instance.ResponseType);
            }

            return mt.Process();
        }

    }

    public static class InstanceExtensions
    {
        public static string GetClientRequestType(this IServiceInstance instance, string coreLibraryName)
        {
            return instance.RequestType.FullName.Replace($"Gtt.Links.Core", $"Gtt.Links.Client");
        }

        public static string GetStateType(this IStatefulServiceInstance instance, string coreLibraryName)
        {
            return instance.StateType.FullName.Replace($"Gtt.Links.Core", $"Gtt.Links.Client");
        }
        public static string GetTriggerType(this IStatefulServiceInstance instance, string coreLibraryName)
        {
            return instance.TriggerType.FullName.Replace($"Gtt.Links.Core", $"Gtt.Links.Client");
        }
        public static string GetDataType(this IStatefulServiceInstance instance, string coreLibraryName)
        {
            return instance.DataType.FullName.Replace($"Gtt.Links.Core", $"Gtt.Links.Client");
        }

        public static string GetClientResponseType(this IServiceInstance instance, string coreLibraryName)
        {
            return instance.ResponseType.GenericTypeArguments[0].FullName.Replace($"Gtt.Links.Core", $"Gtt.Links.Client");
        }
    }
}
