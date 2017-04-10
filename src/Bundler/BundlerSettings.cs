using Bundler.Postprocessors.AutoPrefixer;

namespace Bundler {

    /// <summary>
    /// Options used when bundling files in a bundle.
    /// </summary>
    public class BundlerSettings {

        /// <summary>
        /// Initialize a new instance of the <see cref="BundlerSettings"/> class.
        /// </summary>
        public BundlerSettings() {
            AutoPrefixerOptions = new AutoPrefixerOptions() {
                Enabled = true,
                Browsers = new string[] { "last 2 versions" }
            };
            DaysToKeepFiles = 7;
            OutputPath = "~/bundles";
            WatchFiles = true;
        }

        /// <summary>
        /// Gets ot sets the currently configured settings.
        /// </summary>
        public static BundlerSettings Current { get; internal set; } = new BundlerSettings();

        /// <summary>
        /// Gets or sets the auto prefixer options used when bundling styles.
        /// </summary>
        public AutoPrefixerOptions AutoPrefixerOptions { get; set; }

        /// <summary>
        /// Gets or sets the number of days to keep the files in the <see cref="OutputPath"/> before they are cleaned up.
        /// </summary>
        public int DaysToKeepFiles { get; set; }

        /// <summary>
        /// Gets or sets the directory path where to write bundle files, e.g. ~/bundles.
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to watch files inucluded in a bundle for changes.
        /// </summary>
        public bool WatchFiles { get; set; }
    }
}
