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
            ScriptOutputPath = "~/js";
            StyleOutputPath = "~/css";
            WatchFiles = true;
        }

        /// <summary>
        /// Gets or sets the auto prefixer options used when bundling styles.
        /// </summary>
        public AutoPrefixerOptions AutoPrefixerOptions { get; set; }

        /// <summary>
        /// Gets ot sets the currently configured settings.
        /// </summary>
        public static BundlerSettings Current { get; internal set; } = new BundlerSettings();

        /// <summary>
        /// Gets or sets the number of days to keep output files before they are cleaned up.
        /// </summary>
        public int DaysToKeepFiles { get; set; }

        /// <summary>
        /// Gets or sets the directory path where to write bundled script files, e.g. ~/js.
        /// </summary>
        public string ScriptOutputPath { get; set; }

        /// <summary>
        /// Gets or sets the directory path where to write bundled stylesheets, e.g. ~/css.
        /// </summary>
        public string StyleOutputPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to watch files included in a bundle for changes.
        /// </summary>
        public bool WatchFiles { get; set; }
    }
}
