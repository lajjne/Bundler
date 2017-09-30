using Bundler.Compression;
using Bundler.Postprocessors.AutoPrefixer;
using Bundler.Preprocessors;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Bundler {

    /// <summary>
    /// The css bundler.
    /// </summary>
    public class StyleBundler : BundlerBase {

        /// <summary>
        /// The auto prefixer postprocessor.
        /// </summary>
        private static readonly AutoPrefixerPostprocessor AutoPrefixerPostprocessor = new AutoPrefixerPostprocessor();

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleBundler"/> class.
        /// </summary>
        /// <param name="options">
        /// The options containing instructions for the bundler.
        /// </param>
        /// <param name="context">
        /// The current context.
        /// </param>
        public StyleBundler(BundleOptions options, HttpContext context)
            : base(options, context) {
        }

        /// <summary>
        /// Minifies the specified resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The minified resource.</returns>
        public override string Minify(string resource) {
            CssMinifier minifier;

            if (this.Options.Minify) {
                minifier = new CssMinifier { RemoveWhiteSpace = true };
            } else {
                minifier = new CssMinifier { RemoveWhiteSpace = false };
            }

            return minifier.Minify(resource);
        }

        /// <summary>
        /// Post process the input using auto prefixer.
        /// </summary>
        /// <param name="input">
        /// The input CSS.
        /// </param>
        /// <param name="options">
        /// The <see cref="AutoPrefixerOptions"/>.
        /// </param>
        /// <returns>
        /// The <see cref="string"/> containing the post-processed CSS.
        /// </returns>
        public string AutoPrefix(string input, AutoPrefixerOptions options) {
            return AutoPrefixerPostprocessor.Transform(input, options);
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

            // Preprocess
            contents = this.PreProcessInput(contents, file);

            // Watch file if applicable.
            this.AddFileMonitor(file);

            return contents;
        }

        /// <summary>
        /// Transforms the content of the given string using the correct PreProcessor. 
        /// </summary>
        /// <param name="input">The input string to transform.</param>
        /// <param name="path">The path to the file.</param>
        /// <returns>The transformed string.</returns>
        protected override string PreProcessInput(string input, string path) {
            // Do the base processing then process any specific code here. 
            input = base.PreProcessInput(input, path);

            // Run the last filter. This should be the ResourcePreprocessor.
            input = PreprocessorManager.Instance.Preprocessors
                .First(preprocessor => preprocessor.AllowedExtensions == null)
                .Transform(input, path, this);

            return input;
        }
    }
}
