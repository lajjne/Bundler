using System;
using System.Linq;
using Bundler.Extensions;
using System.Text.RegularExpressions;
using Bundler.Helpers;
using System.IO;
using System.Globalization;

namespace Bundler.Preprocessors.Less {

    /// <summary>
    /// Provides methods to preprocess JS.
    /// </summary>
    public class JavascriptPreprocessor : IPreprocessor {

        /// <summary>
        /// The regular expression to search files for.
        /// </summary>
        private static readonly Regex ImportsRegex = new Regex(@"(?:import\s*([""']?)\s*(?<filename>.+\.js)(\s*[""']?)\s*);", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Gets the extension that this filter processes.
        /// </summary>
        public string[] AllowedExtensions => new[] { ".JS" };

        /// <summary>
        ///  Parses the string for Javascript imports and replaces them with the referenced Javascript.
        /// </summary>
        /// <param name="input">The Javascript to parse for import statements.</param>
        /// <param name="path">The path to the given input string to transform.</param>
        /// <param name="cruncher">The cruncher that is running the transform.</param>
        /// <returns>The transformed string.</returns>
        public string Transform(string input, string path, BundlerBase cruncher) {
            // Check for imports and parse if necessary.
            if (!input.Contains("import", StringComparison.OrdinalIgnoreCase)) {
                return input;
            }

            // Recursively parse the javascript for imports.
            foreach (Match match in ImportsRegex.Matches(input)) {
                // Recursively parse the javascript for imports.
                GroupCollection groups = match.Groups;
                CaptureCollection fileCaptures = groups["filename"].Captures;

                if (fileCaptures.Count > 0) {
                    string fileName = fileCaptures[0].ToString();
                    string importedJavascript = string.Empty;

                    // Check and add the @import the match.
                    FileInfo fileInfo = null;

                    // Try to get the file by absolute/relative path
                    if (!ResourceHelper.IsResourceFilenameOnly(fileName)) {
                        string cssFilePath = ResourceHelper.GetFilePath(fileName, cruncher.Options.RootFolder, cruncher.Context);
                        if (File.Exists(cssFilePath)) {
                            fileInfo = new FileInfo(cssFilePath);
                        }
                    } else {
                        fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(cruncher.Options.RootFolder, fileName)));
                    }

                    // Read the file.
                    if (fileInfo != null && fileInfo.Exists) {
                        string file = fileInfo.FullName;

                        using (StreamReader reader = new StreamReader(file)) {
                            // Parse the children.
                            importedJavascript = Transform(reader.ReadToEnd(), file, cruncher);
                        }

                        // Cache if applicable.
                        cruncher.AddFileMonitor(file, importedJavascript);
                    }

                    // Replace the regex match with the full qualified javascript.
                    input = input.Replace(match.Value, importedJavascript);
                }
            }

            return input;
        }
    }
}
