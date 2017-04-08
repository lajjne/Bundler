namespace Bundler {
    /// <summary>
    /// The cruncher options.
    /// </summary>
    public class BundlerOptions {
        /// <summary>
        /// Gets or sets a value indicating whether to minify the file.
        /// </summary>
        public bool Minify { get; set; }

        /// <summary>
        /// Gets or sets the root folder.
        /// </summary>
        public string RootFolder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to cache files.
        /// </summary>
        public bool CacheFiles { get; set; }

    }
}
