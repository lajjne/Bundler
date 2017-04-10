using Bundler;

namespace Bundler.Web {
    public class BundlerConfig {

        public static void Configure() {
            var settings = new BundlerSettings();
            settings.OutputPath = "~/bundles";
            settings.DaysToKeepFiles = 5;
            settings.WatchFiles = true;
            Bundle.Configure(settings);
        }
    }
}