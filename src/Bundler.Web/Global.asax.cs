using Bundler.Configuration;
using JavaScriptEngineSwitcher.Core;
using System.Web.Mvc;
using System.Web.Routing;

namespace Bundler.Web {
    public class MvcApplication : System.Web.HttpApplication {
        protected void Application_Start() {
            AreaRegistration.RegisterAllAreas();
            JsEngineSwitcherConfig.Configure(JsEngineSwitcher.Instance);
            BundlerConfig.Configure(new BundlerConfig() {
                PhysicalFilesPath = "~/bundles",
                PhysicalFilesDaysBeforeRemoveExpired = 5
            });
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
