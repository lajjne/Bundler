using System;

namespace Bundler.Preprocessors.Sass {

    /// <summary>
    /// Provides methods to convert SASS and SCSS into CSS.
    /// </summary>
    public class SassPreprocessor : IPreprocessor {
        /// <summary>
        /// Gets the extensions that this filter processes.
        /// </summary>
        public string[] AllowedExtensions => new[] { ".sass", ".scss" };

        /// <summary>
        /// Transforms the content of the given string.
        /// </summary>
        /// <param name="input">The input string to transform.</param>
        /// <param name="path">The path to the given input string to transform.</param>
        /// <param name="cruncher">The cruncher that is running the transform.</param>
        /// <returns>The transformed string.</returns>
        public string Transform(string input, string path, BundlerBase cruncher) {
            try {
                var options = new LibSass.Compiler.Options.SassOptions {
                    InputPath = path,
                    OutputStyle = cruncher.Options.Minify ? LibSass.Compiler.Options.SassOutputStyle.Compressed : LibSass.Compiler.Options.SassOutputStyle.Expanded,
                    Precision = 5,
                    IsIndentedSyntax = System.IO.Path.GetExtension(path).Equals(".sass", StringComparison.OrdinalIgnoreCase)
                };
                var compiler = new LibSass.Compiler.SassCompiler(options);
                var result = compiler.Compile();
                foreach (var file in result.IncludedFiles) {
                    cruncher.AddFileMonitor(file, "not empty");
                }
                return result.Output;
            } catch (Exception ex) {
                throw new SassAndScssCompilingException(ex.Message, ex.InnerException);
            }
        }
    }
}
