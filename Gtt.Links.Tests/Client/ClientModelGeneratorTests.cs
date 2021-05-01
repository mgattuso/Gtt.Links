using System;
using System.Collections.Generic;
using System.Text;
using Gtt.CodeWorks;
using Gtt.CodeWorks.Duplicator;
using Gtt.Links.CodeGen;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gtt.Links.Tests.Client
{
    [TestClass]
    public class ClientModelGeneratorTests
    {
        [TestMethod]
        public void CreateModels()
        {
            var c = new ClientGenerator("Links");
            c.Initialize(new GeneratorInitializationContext());
            var models = c.ModelDuplicator();
            Console.WriteLine(models);
        }
    }
}
