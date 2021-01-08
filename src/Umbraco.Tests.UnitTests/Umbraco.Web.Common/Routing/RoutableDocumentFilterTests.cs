using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Moq;
using NUnit.Framework;
using Umbraco.Core.Configuration.Models;
using Umbraco.Core.Hosting;
using Umbraco.Core.IO;
using Umbraco.Web.Common.Routing;

namespace Umbraco.Tests.UnitTests.Umbraco.Web.Common.Routing
{
    [TestFixture]
    public class RoutableDocumentFilterTests
    {
        private GlobalSettings GetGlobalSettings() => new GlobalSettings();

        private IHostingEnvironment GetHostingEnvironment()
        {
            var hostingEnv = new Mock<IHostingEnvironment>();
            hostingEnv.Setup(x => x.ToAbsolute(It.IsAny<string>())).Returns((string virtualPath) => virtualPath.TrimStart('~', '/'));
            return hostingEnv.Object;
        }

        [TestCase("/umbraco/editContent.aspx")]
        [TestCase("/install/default.aspx")]
        [TestCase("/install/")]
        [TestCase("/install")]
        [TestCase("/install/?installStep=asdf")]
        [TestCase("/install/test.aspx")]
        public void Is_Reserved_Path_Or_Url(string url)
        {
            var routableDocFilter = new RoutableDocumentFilter(
                GetGlobalSettings(),
                GetHostingEnvironment(),
                new DefaultEndpointDataSource());

            // Will be false if it is a reserved path
            Assert.IsFalse(routableDocFilter.IsDocumentRequest(url));
        }

        [TestCase("/base/somebasehandler")]
        [TestCase("/")]
        [TestCase("/home.aspx")]
        [TestCase("/umbraco-test")]
        [TestCase("/install-test")]
        [TestCase("/install.aspx")]
        public void Is_Not_Reserved_Path_Or_Url(string url)
        {
            var routableDocFilter = new RoutableDocumentFilter(
                GetGlobalSettings(),
                GetHostingEnvironment(),
                new DefaultEndpointDataSource());

            // Will be true if it's not reserved
            Assert.IsTrue(routableDocFilter.IsDocumentRequest(url));
        }

        [TestCase("/Do/Not/match", false)]
        [TestCase("/Umbraco/RenderMvcs", false)]
        [TestCase("/Umbraco/RenderMvc", true)]
        [TestCase("/umbraco/RenderMvc/Index", true)]
        [TestCase("/Umbraco/RenderMvc/Index/1234", true)]
        [TestCase("/Umbraco/RenderMvc/Index/1234/", true)]
        [TestCase("/Umbraco/RenderMvc/Index/1234/9876", false)]
        [TestCase("/api", true)]
        [TestCase("/api/WebApiTest", true)]
        [TestCase("/Api/WebApiTest/1234", true)]
        [TestCase("/api/WebApiTest/Index/1234", false)]
        public void Is_Reserved_By_Route(string url, bool isReserved)
        {
            var globalSettings = new GlobalSettings { ReservedPaths = string.Empty, ReservedUrls = string.Empty };

            RouteEndpoint endpoint1 = CreateEndpoint(
                "Umbraco/RenderMvc/{action?}/{id?}",
                new { controller = "RenderMvc" },
                "Umbraco_default",
                0);

            RouteEndpoint endpoint2 = CreateEndpoint(
                "api/{controller?}/{id?}",
                new { action = "Index" },
                "WebAPI",
                1);

            var endpointDataSource = new DefaultEndpointDataSource(endpoint1, endpoint2);

            var routableDocFilter = new RoutableDocumentFilter(
                globalSettings,
                GetHostingEnvironment(),
                endpointDataSource);

            Assert.AreEqual(
                !isReserved, // not reserved if it's a document request
                routableDocFilter.IsDocumentRequest(url));
        }

        // borrowed from https://github.com/dotnet/aspnetcore/blob/19559e73da2b6d335b864ed2855dd8a0c7a207a0/src/Mvc/Mvc.Core/test/Routing/ControllerLinkGeneratorExtensionsTest.cs#L171
        private RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            string name = null,
            int order = 0) => new RouteEndpoint(
                (httpContext) => Task.CompletedTask,
                RoutePatternFactory.Parse(template, defaults, null),
                order,
                new EndpointMetadataCollection(Array.Empty<object>()),
                name);
    }
}
