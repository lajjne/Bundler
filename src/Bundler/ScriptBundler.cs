using System.Threading.Tasks;
using System.Web;
using Bundler.Compression;

namespace Bundler {

    /// <summary>
    /// The JavaScript bundler.
    /// </summary>
    public class ScriptBundler : BundlerBase {

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptBundler"/> class.
        /// </summary>
        /// <param name="options">
        /// The options containing instructions for the bundler.
        /// </param>
        /// <param name="context">
        /// The current context.
        /// </param>
        public ScriptBundler(BundleOptions options, HttpContext context)
            : base(options, context) {
        }

        /// <summary>
        /// Minifies the specified resource.
        /// </summary>
        /// <param name="script">The script to minify.</param>
        /// <returns>
        /// The minified resource.
        /// </returns>
        public override string Minify(string script) {
            var minifier = new JavaScriptMinifier {
                LocalRenaming = Options.Minify ? NUglify.JavaScript.LocalRenaming.CrunchAll : NUglify.JavaScript.LocalRenaming.KeepAll,
                PreserveFunctionNames = !Options.Minify,
                RemoveWhiteSpace = Options.Minify
            };

            return minifier.Minify(script);
        }

        /// <summary>
        /// Loads the local file.
        /// </summary>
        /// <param name="file">The file to load.</param>
        /// <returns>
        /// The contents of the local file as a string.
        /// </returns>
        protected override async Task<string> LoadFileAsync(string file) {
            string contents = await base.LoadFileAsync(file);

            contents = this.PreProcessInput(contents, file);

            // Watch file if applicable.
            this.AddFileMonitor(file);

            return contents;
        }

    }
}
