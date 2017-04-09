using Bundler.Configuration;
using JavaScriptEngineSwitcher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Bundler.Web {
    public class MvcApplication : System.Web.HttpApplication {
        protected void Application_Start() {
            AreaRegistration.RegisterAllAreas();
            JsEngineSwitcherConfig.Configure(JsEngineSwitcher.Instance);
            BundlerConfig.Configure(new BundlerConfig());
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
