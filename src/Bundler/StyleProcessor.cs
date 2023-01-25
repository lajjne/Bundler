using AsyncKeyedLock;
using Bundler.Caching;
using Bundler.Extensions;
using Bundler.Helpers;
using Bundler.Preprocessors;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Bundler {

    /// <summary>
    /// The CSS processor for processing CSS files.
    /// </summary>
    public class StyleProcessor : ProcessorBase {

        /// <summary>
        /// Ensures processing is atomic.
        /// </summary>
        private static readonly AsyncKeyedLocker<string> _locker = new AsyncKeyedLocker<string>(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });

        /// <summary>
        /// Processes the css request and returns the result.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="minify">Whether to minify the output.</param>
        /// <param name="paths">The paths to the resources to bundle.</param>
        /// <returns>
        /// The <see cref="string"/> representing the processed result.
        /// </returns>
        public async Task<string> ProcessCssCrunchAsync(HttpContext context, bool minify, params string[] paths) {
            string combinedCSS = string.Empty;

            if (paths != null) {
                string key = string.Concat(paths).ToMd5Fingerprint() + (minify ? Bundler.DOT_MIN : "");

                using (await _locker.LockAsync(key).ConfigureAwait(false)) {
                    combinedCSS = (string)CacheManager.GetItem(key);

                    if (string.IsNullOrWhiteSpace(combinedCSS)) {

                        BundleOptions options = new BundleOptions {
                            Minify = minify,
                            WatchFiles = BundlerSettings.Current.WatchFiles,
                            WatchAlways = BundlerSettings.Current.WatchAlways
                        };

                        StyleBundler bundler = new StyleBundler(options, context);

                        // Expand .bundle files
                        paths = ResourceHelper.ExpandBundles(context, true, null, paths);

                        // Loop through and process each file
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (string path in paths) {

                            // watch .bundle file
                            if (Path.GetExtension(path).Equals(Bundler.DOT_BUNDLE, StringComparison.OrdinalIgnoreCase)) {
                                bundler.AddFileMonitor(path);
                                continue;
                            }

                            if (PreprocessorManager.Instance.AllowedExtensionsRegex.IsMatch(path)) {
                                string filePath = ResourceHelper.GetFilePath(path, options.RootFolder, context);
                                if (File.Exists(filePath)) {
                                    options.RootFolder = Path.GetDirectoryName(filePath);
                                    var result = await bundler.ProcessAsync(filePath);

                                    // minify (unless already minified)
                                    if (minify && !filePath.Contains(Bundler.DOT_MIN, StringComparison.OrdinalIgnoreCase)) {
                                        result = bundler.Minify(result);
                                    }

                                    stringBuilder.Append(result);
                                }
                            }
                        }

                        combinedCSS = stringBuilder.ToString();

                        // apply autoprefixer
                        combinedCSS = bundler.AutoPrefix(combinedCSS, BundlerSettings.Current.AutoPrefixerOptions);

                        AddItemToCache(key, combinedCSS, bundler.FileMonitors);
                    }
                }
            }

            return combinedCSS;
        }
    }
}
