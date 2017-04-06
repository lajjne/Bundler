using Bundler.Extensions;
using Bundler.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Hosting;

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
        /// <param name="fileNames">
        /// The file names, without the directory path, to link to. These can be .css, .less, .sass, and .scss files or an extension-less token representing an
        /// external file of the given formats as configured in the web.config.
        /// </param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(params string[] fileNames) {
            return Styles(null, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a stylesheet link to the bundled css representing the given files.
        /// </summary>
        /// <param name="mediaQuery">
        /// The media query to apply to the link. For reference see:
        /// <a href="https://developer.mozilla.org/en-US/docs/Web/Guide/CSS/Media_queries?redirectlocale=en-US&amp;redirectslug=CSS%2FMedia_queries"/>Media Queries<a/> 
        /// </param>
        /// <param name="fileNames">
        /// The file names, without the directory path, to link to. These can be .css, .less, .sass, and .scss files or an extension-less token representing an
        /// external file of the given formats as configured in the web.config.
        /// </param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the stylesheet link.
        /// </returns>
        public static HtmlString Styles(HtmlString mediaQuery, params string[] fileNames) {
            StringBuilder stringBuilder = new StringBuilder();
            HttpContext context = HttpContext.Current;

            // Minify on release.
            if (!context.IsDebuggingEnabled) {
                string fileContent = AsyncHelper.RunSync(() => StyleProcessor.ProcessCssCrunchAsync(context, true, fileNames));
                string fileName = $"{fileContent.ToMd5Fingerprint()}.css";
                return
                    new HtmlString(
                        string.Format(
                            CssPhysicalFileTemplate,
                            AsyncHelper.RunSync(
                                () => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                            mediaQuery));
            }

            // Render them separately for debug mode.
            foreach (string name in fileNames) {
                string currentName = name;
                string fileContent = AsyncHelper.RunSync(() => StyleProcessor.ProcessCssCrunchAsync(context, false, currentName));
                string fileName = $"{Path.GetFileNameWithoutExtension(name)}.{fileContent.ToMd5Fingerprint()}.css";
                stringBuilder.AppendFormat(
                    CssPhysicalFileTemplate,
                    AsyncHelper.RunSync(() => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                    mediaQuery);
                stringBuilder.AppendLine();
            }

            return new HtmlString(stringBuilder.ToString());
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="fileNames">
        /// The file names, without the directory path, to link to. These can be .js, and .coffee files or an extension-less token representing an
        /// external file of the given formats as configured in the web.config.
        /// </param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the script tag with the correct link.
        /// </returns>
        public static HtmlString Scripts(params string[] fileNames) {
            return Scripts(JavaScriptLoadBehaviour.Inline, fileNames);
        }

        /// <summary>
        /// Renders the correct html to create a script tag linking to the bundled JavaScript representing the given files.
        /// </summary>
        /// <param name="behaviour">
        /// The <see cref="JavaScriptLoadBehaviour"/> describing the way the browser should load the JavaScript into the page.
        /// </param>
        /// <param name="fileNames">
        /// The file names, without the directory path, to link to. These can be .js, and .coffee files or an extension-less token representing an
        /// external file of the given formats as configured in the web.config.
        /// </param>
        /// <returns>
        /// The <see cref="HtmlString"/> containing the script tag with the correct link.
        /// </returns>
        public static HtmlString Scripts(JavaScriptLoadBehaviour behaviour, params string[] fileNames) {
            StringBuilder stringBuilder = new StringBuilder();
            HttpContext context = HttpContext.Current;

            string behaviourParam = behaviour == JavaScriptLoadBehaviour.Inline ? string.Empty : " " + behaviour.ToString().ToLowerInvariant();

            // Minify on release.
            if (!context.IsDebuggingEnabled) {
                string fileContent = AsyncHelper.RunSync(() => ScriptProcessor.ProcessJavascriptCrunchAsync(context, true, fileNames));
                string fileName = $"{fileContent.ToMd5Fingerprint()}.js";
                return
                    new HtmlString(
                        string.Format(
                            JavaScriptPhysicalFileTemplate,
                            AsyncHelper.RunSync(
                                () => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                            behaviourParam));
            }

            // Render them separately for debug mode.
            foreach (string name in fileNames) {
                string currentName = name;
                string fileContent = AsyncHelper.RunSync(() => ScriptProcessor.ProcessJavascriptCrunchAsync(context, false, currentName));
                string fileName = $"{Path.GetFileNameWithoutExtension(name)}.{fileContent.ToMd5Fingerprint()}.js";
                stringBuilder.AppendFormat(
                    JavaScriptPhysicalFileTemplate,
                    AsyncHelper.RunSync(() => ResourceHelper.CreateResourcePhysicalFileAsync(fileName, fileContent)),
                    behaviourParam);
                stringBuilder.AppendLine();
            }

            return new HtmlString(stringBuilder.ToString());
        }
    }
}
