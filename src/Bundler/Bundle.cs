using Bundler.Extensions;
using Bundler.Helpers;
using System.IO;
using System.Text;
using System.Web;

namespace Bundler {

    /// <summary>
    /// Provides methods for rendering CSS and JavaScript links onto a webpage.
    /// </summary>
    public static class Bundle {

        /// <summary>
        /// The template for generating css links pointing to a physical file
        /// </summary>
        private const string CssPhysicalFileTemplate = "<link href=\"{0}\" rel=\"stylesheet\" {1}/>";

        /// <summary>
        /// The template for generating JavaScript links pointing to a physical file
        /// </summary>
        private const string JavaScriptPhysicalFileTemplate = "<script src=\"{0}\"{1}></script>";

        /// <summary>
        /// The CSS handler.
        /// </summary>
        private static readonly StyleProcessor StyleProcessor = new StyleProcessor();

        /// <summary>
        /// The JavaScript handler.
        /// </summary>
        private static readonly ScriptProcessor ScriptProcessor = new ScriptProcessor();

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(params string[] fileNames) {
            return Styles(GetDefaultBundleOptions(), null, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="options">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(BundleOptions options, params string[] fileNames) {
            return Styles(options, null, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="mediaQuery">The media query to apply to the link.</param>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(HtmlString mediaQuery, params string[] fileNames) {
            return Styles(GetDefaultBundleOptions(), mediaQuery, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="options">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <param name="mediaQuery">The media query to apply to the link.</param>
        /// <param name="fileNames">The files to link to. These can be .css, .less, .sass, and .scss files.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(BundleOptions options, HtmlString mediaQuery, params string[] fileNames) {
            bool combined = options == BundleOptions.Combined || options == BundleOptions.MinifiedAndCombined;
            bool minified = options == BundleOptions.Minified || options == BundleOptions.MinifiedAndCombined;
            string ext = minified ? ".min.css" : ".css";

            HttpContext context = HttpContext.Current;
            if (combined) {
                // Combine files
                string fileContent = AsyncHelper.RunSync(() => StyleProcessor.ProcessCssCrunchAsync(context, minified, fileNames));
                if (!string.IsNullOrWhiteSpace(fileContent)) {
                    string fileName = $"{fileContent.ToMd5Fingerprint()}{ext}";
                    return new HtmlString(string.Format(CssPhysicalFileTemplate,
                        AsyncHelper.RunSync(() => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                        mediaQuery));
                }
            } else {
                // Render files separately
                StringBuilder stringBuilder = new StringBuilder();

                // Expand .bundle files
                fileNames = ResourceHelper.ExpandBundles(context, false, null, fileNames);

                foreach (string name in fileNames) {
                    string fileContent = AsyncHelper.RunSync(() => StyleProcessor.ProcessCssCrunchAsync(context, minified, name));
                    if (!string.IsNullOrWhiteSpace(fileContent)) {
                        string fileName = $"{Path.GetFileNameWithoutExtension(name)}.{fileContent.ToMd5Fingerprint()}{ext}";
                        stringBuilder.AppendFormat(CssPhysicalFileTemplate,
                            AsyncHelper.RunSync(() => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                            mediaQuery);
                        stringBuilder.AppendLine();
                    }
                }
                return new HtmlString(stringBuilder.ToString());
            }

            return null;
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileNames">The .js files to link to.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the script tag with the correct link.
        /// </returns>
        public static HtmlString Scripts(params string[] fileNames) {
            return Scripts(GetDefaultBundleOptions(), JavaScriptLoadBehaviour.Inline, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="options">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <param name="fileNames">The .js files to link to.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the script tag with the correct link.
        /// </returns>
        public static HtmlString Scripts(BundleOptions options, params string[] fileNames) {
            return Scripts(options, JavaScriptLoadBehaviour.Inline, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="options">Bundle options deciding how to combine and/or minify the given files.</param>
        /// <param name="behaviour">The <see cref="JavaScriptLoadBehaviour"/> describing the way the browser should load the JavaScript into the page.</param>
        /// <param name="fileNames">The .js files to link to.</param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the script tag with the correct link.
        /// </returns>
        public static HtmlString Scripts(BundleOptions options, JavaScriptLoadBehaviour behaviour, params string[] fileNames) {
            bool combined = options == BundleOptions.Combined || options == BundleOptions.MinifiedAndCombined;
            bool minified = options == BundleOptions.Minified || options == BundleOptions.MinifiedAndCombined;
            string ext = minified ? ".min.js" : ".js";

            HttpContext context = HttpContext.Current;
            string behaviourParam = behaviour == JavaScriptLoadBehaviour.Inline ? string.Empty : " " + behaviour.ToString().ToLowerInvariant();
            if (combined) {
                // Combine files
                string fileContent = AsyncHelper.RunSync(() => ScriptProcessor.ProcessJavascriptCrunchAsync(context, minified, fileNames));
                if (!string.IsNullOrWhiteSpace(fileContent)) {
                    string fileName = $"{fileContent.ToMd5Fingerprint()}{ext}";
                    return
                        new HtmlString(
                            string.Format(
                                JavaScriptPhysicalFileTemplate,
                                AsyncHelper.RunSync(
                                    () => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                                behaviourParam));
                }
            } else {
                // Render files separately
                StringBuilder stringBuilder = new StringBuilder();

                // Expand .bundle files
                fileNames = ResourceHelper.ExpandBundles(context, false, null, fileNames);

                foreach (string name in fileNames) {
                    string fileContent = AsyncHelper.RunSync(() => ScriptProcessor.ProcessJavascriptCrunchAsync(context, false, name));
                    if (!string.IsNullOrWhiteSpace(fileContent)) {
                        string fileName = $"{Path.GetFileNameWithoutExtension(name)}.{fileContent.ToMd5Fingerprint()}{ext}";
                        stringBuilder.AppendFormat(
                            JavaScriptPhysicalFileTemplate,
                            AsyncHelper.RunSync(() => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                            behaviourParam);
                        stringBuilder.AppendLine();
                    }
                }
                return new HtmlString(stringBuilder.ToString());
            }

            return null;
        }

        /// <summary>
        /// If debugging is enabled in web.config, renders individual unminified files, otherwise combines and minifies files into a single file.
        /// </summary>
        /// <returns></returns>
        private static BundleOptions GetDefaultBundleOptions() {
            return HttpContext.Current.IsDebuggingEnabled ? BundleOptions.Normal : BundleOptions.MinifiedAndCombined;
        }
    }
}
