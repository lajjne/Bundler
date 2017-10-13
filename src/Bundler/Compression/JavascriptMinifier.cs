using NUglify;
using NUglify.JavaScript;

namespace Bundler.Compression {

    /// <summary>
    /// Helper class for performing minification of JavaScript.
    /// </summary>
    public sealed class JavaScriptMinifier {

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Bundler.Compression.JavaScriptMinifier">JavaScriptMinifier</see> class. 
        /// </summary>
        public JavaScriptMinifier() {
            RemoveWhiteSpace = true;
            PreserveFunctionNames = false;
            LocalRenaming = LocalRenaming.CrunchAll;
        }

        /// <summary>
        /// Gets or sets whether this Minifier instance should minify local-scoped variables.
        /// </summary>
        /// <remarks>
        /// Setting this value to CrunchAll can have a negative impact on some scripts, i.e. a pre-minified jQuery will fail if passed through this.
        /// </remarks>
        public LocalRenaming LocalRenaming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this minifier instance should preserve function names when minifying a script.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this minifier instance should preserve function names when minifying a script; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>Scripts that have external scripts relying on their functions should leave this set to true.</remarks>
        public bool PreserveFunctionNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whitespace should be removed from the script.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if whitespace should be removed from the script; otherwise, <see langword="false"/>.
        /// </value>
        public bool RemoveWhiteSpace { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance of the minifier should minify the code.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance of the minifier should minify the code; otherwise, <see langword="false"/>.
        /// </value>
        private bool ShouldMinifyCode => !PreserveFunctionNames || LocalRenaming != LocalRenaming.KeepAll;

        /// <summary>
        /// Gets a value indicating whether this instance of the minifier should perform the minification.
        /// </summary>
        /// <value><see langword="true"/> if the minifier should perform the minification; otherwise, <see langword="false"/>.</value>
        private bool ShouldMinify => RemoveWhiteSpace || ShouldMinifyCode;

        /// <summary>
        /// Gets the minified version of the submitted script.
        /// </summary>
        /// <param name="script">The script to minify.</param>
        /// <returns>The minified version of the submitted script.</returns>
        public string Minify(string script) {
            if (ShouldMinify) {
                if (string.IsNullOrWhiteSpace(script)) {
                    return string.Empty;
                }

                return Uglify.Js(script, CreateCodeSettings()).Code;
            }

            return script;
        }

        /// <summary>
        /// Builds the required CodeSettings class needed for the Ajax Minifier.
        /// </summary>
        /// <returns>The required CodeSettings class needed for the Ajax Minifier.</returns>
        private CodeSettings CreateCodeSettings() {
            CodeSettings codeSettings = new CodeSettings {
                MinifyCode = ShouldMinifyCode,
                OutputMode = RemoveWhiteSpace ? OutputMode.SingleLine : OutputMode.MultipleLines,
            };

            if (ShouldMinifyCode) {
                // determine variable renaming
                codeSettings.LocalRenaming = LocalRenaming;

                // a lot of scripts use eval to parse out various functions and objects, these names need to be kept consistent with the actual arguments
                codeSettings.EvalTreatment = EvalTreatment.Ignore;

                // this makes sure that function names on objects are kept exactly as they are (so functions that other non-minified scripts rely on do not get renamed)
                codeSettings.PreserveFunctionNames = PreserveFunctionNames;

                // specifies whether or not important comments will be retained in the output
                codeSettings.PreserveImportantComments = false;
            }

            return codeSettings;
        }
    }
}
