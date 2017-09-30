using System;

namespace Bundler.Preprocessors.Sass {

    /// <summary>
    /// Provides methods to compile SASS and SCSS into CSS.
    /// </summary>
    public class SassPreprocessor : IPreprocessor {

        /// <summary>
        /// File extension for sass files.
        /// </summary>
        private const string DOT_SASS = ".sass";

        /// <summary>
        /// File extension for scss files.
        /// </summary>
        private const string DOT_SCSS = ".scss";

        /// <summary>
        /// Gets the extensions that this filter processes.
        /// </summary>
        public string[] AllowedExtensions => new[] { DOT_SASS, DOT_SCSS };

        /// <summary>
        /// Transforms the content of the given string.
        /// </summary>
        /// <param name="input">The input string to transform.</param>
        /// <param name="path">The path to the given input string to transform.</param>
        /// <param name="bundler">The bundler that is running the transform.</param>
        /// <returns>The transformed string.</returns>
        public string Transform(string input, string path, BundlerBase bundler) {
            try {
                var options = new LibSass.Compiler.Options.SassOptions {
                    InputPath = path,
                    OutputStyle = LibSass.Compiler.Options.SassOutputStyle.Expanded, // StyleProcessor minifies the compiled css if needed
                    Precision = 5,
                    IsIndentedSyntax = System.IO.Path.GetExtension(path).Equals(DOT_SASS, StringComparison.OrdinalIgnoreCase)
                };
                var compiler = new LibSass.Compiler.SassCompiler(options);
                var result = compiler.Compile();
                if (result.ErrorStatus != 0) {
                    throw new Exception(result.ErrorMessage);
                }

                foreach (var file in result.IncludedFiles) {
                    bundler.AddFileMonitor(file);
                }
                return result.Output;
            } catch (Exception ex) {
                if (ex.InnerException == null) {
                    throw ex;
                } else {
                    throw new Exception(ex.Message, ex.InnerException);
                }
            }
        }
    }
}
