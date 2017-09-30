using Bundler.Caching;
using Bundler.Extensions;
using Bundler.Helpers;
using Bundler.Preprocessors;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Bundler {

    /// <summary>
    /// The JavaScript processor for processing JavaScript files.
    /// </summary>
    public class ScriptProcessor : ProcessorBase {
        /// <summary>
        /// Ensures processing is atomic.
        /// </summary>
        private static readonly AsyncDuplicateLock Locker = new AsyncDuplicateLock();

        /// <summary>
        /// Processes the JavaScript request and returns the result.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="minify">Whether to minify the output.</param>
        /// <param name="paths">The paths to the resources to crunch. </param>
        /// <returns>The <see cref="string"/> representing the processed result.</returns>
        public async Task<string> ProcessJavascriptCrunchAsync(HttpContext context, bool minify, params string[] paths) {
            string combinedJavaScript = string.Empty;

            if (paths != null) {
                string key = string.Concat(paths).ToMd5Fingerprint() + (minify ? Bundler.DOT_MIN : "");

                using (await Locker.LockAsync(key)) {
                    combinedJavaScript = (string)CacheManager.GetItem(key);

                    if (string.IsNullOrWhiteSpace(combinedJavaScript)) {

                        BundleOptions options = new BundleOptions {
                            Minify = minify,
                            WatchFiles = BundlerSettings.Current.WatchFiles
                        };

                        ScriptBundler bundler = new ScriptBundler(options, context);

                        // Expand .bundle files
                        paths = ResourceHelper.ExpandBundles(context, true, null, paths);

                        // Loop through and process each file
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (string path in paths) {

                            // Watch .bundle file
                            if (Path.GetExtension(path).Equals(Bundler.DOT_BUNDLE, StringComparison.OrdinalIgnoreCase)) {
                                bundler.AddFileMonitor(path);
                                continue;
                            }

                            if (PreprocessorManager.Instance.AllowedExtensionsRegex.IsMatch(path)) {
                                string filePath = ResourceHelper.GetFilePath(path, null, context);
                                if (File.Exists(filePath)) {
                                    options.RootFolder = Path.GetDirectoryName(filePath);
                                    var result = await bundler.ProcessAsync(filePath);

                                    // Minify (unless already minified)
                                    if (minify && !filePath.Contains(Bundler.DOT_MIN, StringComparison.OrdinalIgnoreCase)) {
                                        result = bundler.Minify(result);
                                    }

                                    stringBuilder.Append(result);

                                    if (!result.TrimEnd().EndsWith(";")) {
                                        // add semi-colon and new line to avoid problem when combining scripts 
                                        stringBuilder.AppendLine(";");
                                    }

                                }
                            }
                        }

                        combinedJavaScript = stringBuilder.ToString();

                        AddItemToCache(key, combinedJavaScript, bundler.FileMonitors);
                    }
                }
            }

            return combinedJavaScript;
        }
    }
}
