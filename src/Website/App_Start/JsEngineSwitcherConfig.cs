using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.V8;

namespace Website {
    public class JsEngineSwitcherConfig {

        public static void Configure(JsEngineSwitcher engineSwitcher) {
            engineSwitcher.EngineFactories.AddV8();
            engineSwitcher.DefaultEngineName = V8JsEngine.EngineName;
        }
    }
}