using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtt.CodeWorks;

namespace Gtt.Links.Core
{
    public class HealthCheckService : BaseServiceInstance<HealthCheckRequest, HealthCheckResponse>
    {
        public HealthCheckService(CoreDependencies coreDependencies) : base(coreDependencies)
        {
        }

        protected override Task<ServiceResponse<HealthCheckResponse>> Implementation(HealthCheckRequest request, CancellationToken cancellationToken)
        {
            return SuccessfulTask(new HealthCheckResponse());
        }

        protected override Task<string> CreateDistributedLockKey(HealthCheckRequest request, CancellationToken cancellationToken)
        {
            return NoDistributedLock();
        }

        protected override IDictionary<int, string> DefineErrorCodes()
        {
            return NoErrorCodes();
        }
    }

    public class HealthCheckRequest : BaseRequest
    {

    }

    public class HealthCheckResponse
    {

    }
}
