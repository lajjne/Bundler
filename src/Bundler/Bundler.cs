using Bundler.Extensions;
using Bundler.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace Bundler {

    /// <summary>
    /// Provides methods for rendering CSS and JavaScript links onto a webpage.
    /// </summary>
    public static class Bundler {

        /// <summary>
        /// File extension for bundle files.
        /// </summary>
        internal const string DOT_BUNDLE = ".bundle";

        /// <summary>
        /// File extension for css output files.
        /// </summary>
        internal const string DOT_CSS = ".css";

        /// <summary>
        /// File extension for js output files.
        /// </summary>
        internal const string DOT_JS = ".js";

        /// <summary>
        /// File extension for minified output files.
        /// </summary>
        internal const string DOT_MIN = ".min";

        /// <summary>
        /// The template for generating css links pointing to a physical file.
        /// </summary>
        private const string LINK_TEMPLATE = @"<link href=""{0}"" rel=""stylesheet"" {1}/>";

        /// <summary>
        /// The template for generating JavaScript links pointing to a physical file.
        /// </summary>
        private const string SCRIPT_TEMPLATE = @"<script src=""{0}""{1}></script>";

        /// <summary>
        /// The CSS processor.
        /// </summary>
        internal static readonly StyleProcessor StyleProcessor = new StyleProcessor();

        /// <summary>
        /// The JavaScript processor.
        /// </summary>
        internal static readonly ScriptProcessor ScriptProcessor = new ScriptProcessor();

        /// <summary>
        /// Configure Bundler to use the specified settings.
        /// </summary>
        /// <param name="settings"></param>
        public static void Configure(BundlerSettings settings) {
            BundlerSettings.Current = settings;
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileNames">The .js files to create a script tags for.</param>
        /// <returns>A <see cref="HtmlString"/> containing the script tag(s) for the specified javascript files.</returns>
        public static HtmlString Scripts(params string[] fileNames) {
            return Scripts(fileNames, null);
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given file.
        /// </summary>
        /// <param name="fileName">The .js file to create a script tag for.</param>
        /// <param name="htmlAttributes">Anonymous object with hmtl attributes</param>
        /// <param name="output">Decides how to combine and/or minify the given file.</param>
        /// <returns>A <see cref="HtmlString"/> containing the script tag for javascript file.</returns>
        public static HtmlString Scripts(string fileName, object htmlAttributes = null, BundleOutput? output = null) {
            return Scripts(new string[] { fileName }, htmlAttributes, output);
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileNames">The .js files to create script tag(s) for.</param>
        /// <param name="htmlAttributes"></param>
        /// <param name="output">Decides how to combine and/or minify the given file.</param>
        /// <returns>A <see cref="HtmlString"/> containing the script tag(s) for the specified javascript files.</returns>
        public static HtmlString Scripts(string[] fileNames, object htmlAttributes = null, BundleOutput? output = null) {
            output = output ?? GetDefaultBundleOutput();
            var attributes = htmlAttributes.ToNameValueCollection().AsHtmlAttributes(prefix: " ");
            StringBuilder sb = new StringBuilder();
            foreach (var url in ScriptUrls(fileNames, output.Value)) {
                sb.AppendFormat(SCRIPT_TEMPLATE, url, attributes).AppendLine();
            }
            return new HtmlString(sb.ToString().Trim());
        }

        /// <summary>
        /// Returns the urls linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileNames">The .js files to bundle.</param>
        /// <returns>The urls to the bundled script files.</returns>
        public static IEnumerable<string> ScriptUrls(params string[] fileNames) {
            return ScriptUrls(fileNames, null);
        }

        /// <summary>
        /// Returns the urls linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileName">The .js file to bundle.</param>
        /// <param name="output">Decides how to combine and/or minify the given file.</param>
        /// <returns>The url to the bundled script file.</returns>
        public static IEnumerable<string> ScriptUrls(string fileName, BundleOutput? output = null) {
            return ScriptUrls(new[] { fileName }, output);
        }

        /// <summary>
        /// Returns the urls linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileNames">The .js files to bundle.</param>
        /// <param name="output">Decides how to combine and/or minify the given file.</param>
        /// <returns>The urls to the bundled script files.</returns>
        public static IEnumerable<string> ScriptUrls(string[] fileNames, BundleOutput? output = null) {
            output = output ?? GetDefaultBundleOutput();

            bool combined = output == BundleOutput.Combined || output == BundleOutput.MinifiedAndCombined;
            bool minified = output == BundleOutput.Minified || output == BundleOutput.MinifiedAndCombined;
            string ext = minified ? DOT_MIN + DOT_JS : DOT_JS;
            var context = HttpContext.Current;
            if (combined) {
                // combine files
                string fileContent = AsyncHelper.RunSync(() => ScriptProcessor.ProcessJavascriptCrunchAsync(context, minified, fileNames));
                if (!string.IsNullOrWhiteSpace(fileContent)) {
                    string fileName = $"{fileContent.ToMd5Fingerprint()}{ext}";
                    yield return AsyncHelper.RunSync(() => ResourceHelper.GetOrCreateFileAsync(fileName, fileContent));
                }
            } else {
                // render file(s) individually (but first expand .bundle files)
                fileNames = ResourceHelper.ExpandBundles(context, false, null, fileNames);
                foreach (string name in fileNames) {
                    string fileContent = AsyncHelper.RunSync(() => ScriptProcessor.ProcessJavascriptCrunchAsync(context, minified, name));
                    if (!string.IsNullOrWhiteSpace(fileContent)) {
                        string fileName = $"{Path.GetFileNameWithoutExtension(name)}.{fileContent.ToMd5Fingerprint()}{ext}";
                        yield return AsyncHelper.RunSync(() => ResourceHelper.GetOrCreateFileAsync(fileName, fileContent));
                    }
                }
            }
        }

        /// <summary>
        /// Renders the correct html to create stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <returns>A <see cref="HtmlString"/> containing the stylesheet links.</returns>
        public static HtmlString Styles(params string[] fileNames) {
            return Styles(fileNames, null, null);
        }

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileName">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <param name="htmlAttributes"></param>
        /// <param name="output">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <returns>A <see cref="HtmlString"/> containing the stylesheet link.</returns>
        public static HtmlString Styles(string fileName, object htmlAttributes = null, BundleOutput? output = null) {
            return Styles(new[] { fileName }, htmlAttributes, output);
        }

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <param name="htmlAttributes"></param>
        /// <param name="output">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(string[] fileNames, object htmlAttributes = null, BundleOutput? output = null) {
            output = output ?? GetDefaultBundleOutput();
            var attributes = htmlAttributes.ToNameValueCollection().AsHtmlAttributes(prefix: " ");
            StringBuilder sb = new StringBuilder();
            foreach (var url in StyleUrls(fileNames, output.Value)) {
                sb.AppendFormat(LINK_TEMPLATE, url, attributes).AppendLine();
            }
            return new HtmlString(sb.ToString().Trim());
        }


        /// <summary>
        /// Returns the urls linking to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <returns>The urls to the bundled stylesheet files.</returns>
        public static IEnumerable<string> StyleUrls(params string[] fileNames) {
            return StyleUrls(fileNames, null);
        }

        /// <summary>
        /// Returns the urls linking to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileName">The file to link to. It can be a .css, .less, .sass, or .scss file.</param>
        /// <param name="output">Decides how to combine and/or minify the given file.</param>
        /// <returns>The url to the bundled stylesheet file.</returns>
        public static IEnumerable<string> StyleUrls(string fileName, BundleOutput? output = null) {
            return StyleUrls(new[] { fileName }, output);
        }

        /// <summary>
        /// Returns the urls linking to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <param name="output">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <returns>The urls to the bundled stylesheet files.</returns>
        public static IEnumerable<string> StyleUrls(string[] fileNames, BundleOutput? output = null) {
            output = output ?? GetDefaultBundleOutput();

            bool combined = output == BundleOutput.Combined || output == BundleOutput.MinifiedAndCombined;
            bool minified = output == BundleOutput.Minified || output == BundleOutput.MinifiedAndCombined;
            string ext = minified ? DOT_MIN + DOT_CSS : DOT_CSS;

            HttpContext context = HttpContext.Current;
            if (combined) {
                // combine files
                string fileContent = AsyncHelper.RunSync(() => StyleProcessor.ProcessCssCrunchAsync(context, minified, fileNames));
                if (!string.IsNullOrWhiteSpace(fileContent)) {
                    string fileName = $"{fileContent.ToMd5Fingerprint()}{ext}";
                    yield return AsyncHelper.RunSync(() => ResourceHelper.GetOrCreateFileAsync(fileName, fileContent));
                }
            } else {
                // render file(s) individually (but first expand .bundle files)
                StringBuilder stringBuilder = new StringBuilder();
                fileNames = ResourceHelper.ExpandBundles(context, false, null, fileNames);
                foreach (string name in fileNames) {
                    string fileContent = AsyncHelper.RunSync(() => StyleProcessor.ProcessCssCrunchAsync(context, minified, name));
                    if (!string.IsNullOrWhiteSpace(fileContent)) {
                        string fileName = $"{Path.GetFileNameWithoutExtension(name)}.{fileContent.ToMd5Fingerprint()}{ext}";
                        yield return AsyncHelper.RunSync(() => ResourceHelper.GetOrCreateFileAsync(fileName, fileContent));
                    }
                }
            }
        }

        /// <summary>
        /// If debugging is enabled in web.config, renders individual unminified files, otherwise combines and minifies files into a single file.
        /// </summary>
        /// <returns></returns>
        private static BundleOutput GetDefaultBundleOutput() {
            return HttpContext.Current.IsDebuggingEnabled ? BundleOutput.Normal : BundleOutput.MinifiedAndCombined;
        }
    }
}
