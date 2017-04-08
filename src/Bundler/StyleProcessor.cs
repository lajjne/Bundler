using Bundler.Caching;
using Bundler.Configuration;
using Bundler.Extensions;
using Bundler.Helpers;
using Bundler.Preprocessors;
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
        private static readonly AsyncDuplicateLock Locker = new AsyncDuplicateLock();

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
                string key = string.Join(string.Empty, paths).ToMd5Fingerprint();

                using (await Locker.LockAsync(key)) {
                    combinedCSS = (string)CacheManager.GetItem(key);

                    if (string.IsNullOrWhiteSpace(combinedCSS)) {
                        StringBuilder stringBuilder = new StringBuilder();

                        BundlerOptions cruncherOptions = new BundlerOptions {
                            MinifyCacheKey = key,
                            Minify = minify,
                            CacheFiles = true
                        };

                        StyleBundler bundler = new StyleBundler(cruncherOptions, context);

                        // Expand .bundle files
                        paths = ResourceHelper.ExpandBundles(context, true, null, paths);

                        // Loop through and process each file
                        foreach (string path in paths) {

                            // Monitor .bundle file
                            if (Path.GetExtension(path) == ".bundle") {
                                bundler.AddFileMonitor(path, "not empty");
                                continue;
                            }

                            if (PreprocessorManager.Instance.AllowedExtensionsRegex.IsMatch(path)) {
                                string filePath = ResourceHelper.GetFilePath(path, cruncherOptions.RootFolder, context);
                                if (File.Exists(filePath)) {
                                    cruncherOptions.RootFolder = Path.GetDirectoryName(filePath);
                                    stringBuilder.Append(await bundler.CrunchAsync(filePath));
                                }
                            }
                        }

                        combinedCSS = stringBuilder.ToString();

                        // Apply autoprefixer
                        combinedCSS = bundler.AutoPrefix(combinedCSS, BundlerConfiguration.Instance.AutoPrefixerOptions);

                        //// Minify
                        //if (minify) {
                        //    combinedCSS = bundler.Minify(combinedCSS);
                        //}

                        this.AddItemToCache(key, combinedCSS, bundler.FileMonitors);
                    }
                }
            }

            return combinedCSS;
        }
    }
}
