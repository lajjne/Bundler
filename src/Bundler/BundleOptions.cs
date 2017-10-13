namespace Bundler {

    /// <summary>
    /// Options used when bundling files in a bundle.
    /// </summary>
    public class BundleOptions {
        /// <summary>
        /// Gets or sets a value indicating whether to minify the files in the bundle.
        /// </summary>
        public bool Minify { get; set; }

        /// <summary>
        /// Gets or sets the root folder for the bundle and/or current file being processed.
        /// </summary>
        public string RootFolder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to watch files included in the bundle for changes.
        /// </summary>
        public bool WatchFiles { get; set; }

        /// <summary>
        /// Gets or sets a list of files to always watch (even when <see cref="WatchFiles" /> is <c>false</c>).
        /// </summary>
        public string[] WatchAlways { get; set; }
    }
}
