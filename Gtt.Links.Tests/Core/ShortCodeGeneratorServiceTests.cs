using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gtt.CodeWorks;
using Gtt.Links.Core.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gtt.Links.Tests.Core
{
    [TestClass]
    public class ShortCodeGeneratorServiceTests
    {
        [TestMethod]
        public async Task HappyPath()
        {
            var svc = new ShortCodeGeneratorService(CoreDependenciesWithValidation.Instance);
            var req = new ShortCodeGeneratorRequest
            {
                Length = 4
            };
            var res = await svc.Execute(req, CancellationToken.None);

            Assert.IsTrue(res.IsSuccessful());
            Assert.AreEqual(4, res.Data.Code.Length);
        }

        [TestMethod]
        public void NoErrorCodesDefined()
        {
            var svc = new ShortCodeGeneratorService(CoreDependenciesWithValidation.Instance);
            Assert.AreEqual(0, svc.AllErrorCodes().Count());
        }

        [TestMethod]
        public async Task HappyPathWithLength()
        {
            var svc = new ShortCodeGeneratorService(CoreDependenciesWithValidation.Instance);
            var req = new ShortCodeGeneratorRequest
            {
                Length = 8
            };
            var res = await svc.Execute(req, CancellationToken.None);

            Assert.IsTrue(res.IsSuccessful());
            Assert.AreEqual(8, res.Data.Code.Length);
        }

        [TestMethod]
        public async Task HappyPathWithCustomMap()
        {
            var svc = new ShortCodeGeneratorService(CoreDependenciesWithValidation.Instance);
            var req = new ShortCodeGeneratorRequest
            {
                Length = 32,
                CustomLetterMap = "ab"
            };
            var res = await svc.Execute(req, CancellationToken.None);

            Assert.IsTrue(res.IsSuccessful());
            Assert.AreEqual(32, res.Data.Code.Length);
            Assert.AreEqual("ab", string.Join("", res.Data.Code.ToCharArray().Distinct().OrderBy(x => x).ToArray()));
        }

        [TestMethod]
        public async Task HappyPathTwoRequestsResultInDifferentCodes()
        {
            var svc = new ShortCodeGeneratorService(CoreDependenciesWithValidation.Instance);
            var req = new ShortCodeGeneratorRequest
            {
                Length = 4
            };
            var res1 = await svc.Execute(req,  CancellationToken.None);
            var res2 = await svc.Execute(req,  CancellationToken.None);


            Assert.IsTrue(res1.IsSuccessful());
            Assert.IsTrue(res2.IsSuccessful());
            Assert.AreNotEqual(res1.Data.Code, res2.Data.Code);
        }

        [TestMethod]
        public async Task HappyPathSeedGeneratesSameOutput()
        {
            var svc = new ShortCodeGeneratorService(CoreDependenciesWithValidation.Instance);
            var req = new ShortCodeGeneratorRequest
            {
                Length = 4,
                Debug = new ShortCodeGeneratorRequest.DebugData
                {
                    RandomSeed = 34
                }
            };
            var res1 = await svc.Execute(req, CancellationToken.None);
            var res2 = await svc.Execute(req, CancellationToken.None);


            Assert.IsTrue(res1.IsSuccessful());
            Assert.IsTrue(res2.IsSuccessful());
            Assert.AreEqual("EXHY", res1.Data.Code);
            Assert.AreEqual(res1.Data.Code, res2.Data.Code);
        }
    }
}
