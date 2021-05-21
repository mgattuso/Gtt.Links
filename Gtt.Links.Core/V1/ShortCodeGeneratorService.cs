using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtt.CodeWorks;

namespace Gtt.Links.Core.V1
{
    public class ShortCodeGeneratorService : BaseServiceInstance<ShortCodeGeneratorRequest, ShortCodeGeneratorResponse>
    {
        private static readonly char[] LetterMap = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToArray();

        public ShortCodeGeneratorService(CoreDependencies coreDependencies) : base(coreDependencies)
        {
        }

        protected override Task<ServiceResponse<ShortCodeGeneratorResponse>> Implementation(ShortCodeGeneratorRequest request, CancellationToken cancellationToken)
        {
            Random r = new Random(request.Debug?.RandomSeed ?? Guid.NewGuid().GetHashCode());
            bool hasCustomMap = !string.IsNullOrWhiteSpace(request.CustomLetterMap);
            char[] letterMap = LetterMap;
            if (hasCustomMap)
            {
                letterMap = request.CustomLetterMap
                    .ToCharArray()
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();
            }

            string code = "";
            for (int i = 0; i < request.Length; i++)
            {
                var idx = r.Next(0, letterMap.Length);
                code = code + letterMap[idx];
            }

            return SuccessfulTask(new ShortCodeGeneratorResponse
            {
                Code = code
            });
        }

        protected override Task<string> CreateDistributedLockKey(ShortCodeGeneratorRequest request, CancellationToken cancellationToken)
        {
            return NoDistributedLock();
        }

        protected override IDictionary<int, string> DefineErrorCodes()
        {
            return NoErrorCodes();
        }
    }

    public class ShortCodeGeneratorRequest : BaseRequest
    {
        [Range(2, 32)]
        public int Length { get; set; }
        /// <summary>
        /// Implement a custom map of letters to be used to create the
        /// shortcode. Map should consist of a string of unique letters,
        /// numbers, symbols, and characters
        /// </summary>
        public string CustomLetterMap { get; set; }

        public DebugData Debug { get; set; }

        public class DebugData
        {
            public int? RandomSeed { get; set; }
        }
    }

    public class ShortCodeGeneratorResponse
    {
        public string Code { get; set; }
    }
}