using System;
using System.Linq;
using Bundler.Extensions;
using System.Text.RegularExpressions;
using Bundler.Helpers;
using System.IO;
using System.Globalization;

namespace Bundler.Preprocessors.Css {

    /// <summary>
    /// Provides methods to preprocess CSS.
    /// </summary>
    public class CssPreprocessor : IPreprocessor {

        /// <summary>
        /// The regular expression to search files for.
        /// </summary>
        private static readonly Regex ImportsRegex = new Regex(@"((?:@import\s*(url\([""']?)\s*(?<filename>.*\.\w+ss)(\s*[""']?)\s*\))((?<media>([^;@]+))?);)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Gets the extension that this filter processes.
        /// </summary>
        public string[] AllowedExtensions => new[] { Bundler.DOT_CSS };

        /// <summary>
        /// Parses the string for CSS imports and replaces them with the referenced CSS. Also minifies the CSS if needed.
        /// </summary>
        /// <param name="input">The input string to transform.</param>
        /// <param name="path">The path to the given input string to transform.</param>
        /// <param name="bundler">The bundler that is running the transform.</param>
        /// <returns>The transformed string.</returns>
        public string Transform(string input, string path, BundlerBase bundler) {
            // Check for imports and parse if necessary.
            if (input.Contains("@import", StringComparison.OrdinalIgnoreCase)) {

                // Recursively parse the css for imports.
                foreach (Match match in ImportsRegex.Matches(input)) {
                    // Recursively parse the css for imports.
                    GroupCollection groups = match.Groups;
                    CaptureCollection fileCaptures = groups["filename"].Captures;

                    if (fileCaptures.Count > 0) {
                        string fileName = fileCaptures[0].ToString();
                        CaptureCollection mediaQueries = groups["media"].Captures;
                        Capture mediaQuery = null;

                        if (mediaQueries.Count > 0) {
                            mediaQuery = mediaQueries[0];
                        }

                        string importedCss = string.Empty;

                        if (!fileName.Contains(Uri.SchemeDelimiter)) {

                            // Read the file.
                            FileInfo fileInfo = new FileInfo(ResourceHelper.GetFilePath(fileName, bundler.Options.RootFolder, bundler.Context));
                            if (fileInfo.Exists) {
                                string file = fileInfo.FullName;

                                using (StreamReader reader = new StreamReader(file)) {
                                    // Parse the children.
                                    if (mediaQuery != null) {
                                        importedCss = string.Format(CultureInfo.InvariantCulture, "@media {0}{{{1}{2}{1}}}", mediaQuery, Environment.NewLine, Transform(reader.ReadToEnd(), file, bundler));
                                    } else {
                                        importedCss = Transform(reader.ReadToEnd(), file, bundler);
                                    }
                                }

                                // Run the last filter. This should be the ResourcePreprocessor.
                                importedCss = PreprocessorManager.Instance.Preprocessors
                                    .First(preprocessor => preprocessor.AllowedExtensions == null)
                                    .Transform(importedCss, file, bundler);

                                // Watch file
                                bundler.AddFileMonitor(file);
                            }

                            // Replace the regex match with the full qualified css.
                            input = input.Replace(match.Value, importedCss);
                        }
                    }
                }
            }

            return input;
        }
    }
}
