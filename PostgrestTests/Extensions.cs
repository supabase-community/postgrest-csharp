using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Postgrest.Extensions;

namespace PostgrestTests
{
    [TestClass]
    public class Extensions
    {
        [TestMethod]
        public void UriExtensionGetBaseUri()
        {
            var uri1 = new Uri("https://abcdefg.supabase.io/testing/123?query=me-big-query");

            Assert.AreEqual("https://abcdefg.supabase.io", uri1.GetBaseUrl());

            var uri2 = new Uri("http://localhost:3000/testing/123?query=me-big-query");

            Assert.AreEqual("http://localhost:3000", uri2.GetBaseUrl());
        }
    }
}
