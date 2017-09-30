using Bundler;

namespace Website {
    public class BundlerConfig {

        public static void Configure() {
            var settings = new BundlerSettings();
            settings.ScriptOutputPath = "~/js";
            settings.StyleOutputPath = "~/css";
            settings.DaysToKeepFiles = 5;
            settings.WatchFiles = true;
            Bundler.Bundler.Configure(settings);
        }
    }
}