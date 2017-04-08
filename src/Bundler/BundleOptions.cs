namespace Bundler {
    /// <summary>
    /// 
    /// </summary>
    public enum BundleOptions {
        /// <summary>
        /// Left as individual unminified files.
        /// </summary>
        Normal,
        /// <summary>
        /// Left as individual minified files.
        /// </summary>
        Minified,
        /// <summary>
        /// Combined into single unminified file.
        /// </summary>
        Combined,
        /// <summary>
        /// Combined and Minified into a single file.
        /// </summary>
        MinifiedAndCombined
    }
}
