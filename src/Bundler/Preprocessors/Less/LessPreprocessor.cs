using dotless.Core;
using dotless.Core.Importers;
using dotless.Core.Input;
using dotless.Core.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace Bundler.Preprocessors.Less {

    /// <summary>
    /// Provides methods to compile LESS into CSS.
    /// </summary>
    public class LessPreprocessor : IPreprocessor {

        /// <summary>
        /// File extension for less files.
        /// </summary>
        private string DOT_LESS = ".less";

        /// <summary>
        /// Gets the extension that this filter processes.
        /// </summary>
        public string[] AllowedExtensions => new[] { DOT_LESS };

        /// <summary>
        /// Transforms the content of the given string from Less into CSS. 
        /// </summary>
        /// <param name="input">The input string to transform.</param>
        /// <param name="path">The path to the given input string to transform.</param>
        /// <param name="bundler">The bundler that is running the transform.</param>
        /// <returns>The transformed string.</returns>
        public string Transform(string input, string path, BundlerBase bundler) {
            // The standard engine returns a FileNotFoundExecption so I've rolled my own path resolver.
            Parser parser = new Parser();
            LessPathResolver dotLessPathResolver = new LessPathResolver(path);
            FileReader fileReader = new FileReader(dotLessPathResolver);
            parser.Importer = new Importer(fileReader);

            var lessEngine = new LessEngine(parser) {
                KeepFirstSpecialComment = !bundler.Options.Minify,
                DisableVariableRedefines = false,
                Compress = false
            };

            try {
                string result = lessEngine.TransformToCss(input, path);
                if (!lessEngine.LastTransformationSuccessful) {
                    throw lessEngine.LastTransformationError; 
                }

                if (bundler.Options.WatchFiles) {
                    // Add each import as a file dependency
                    IEnumerable<string> imports = lessEngine.GetImports();
                    IList<string> enumerable = imports as IList<string> ?? imports.ToList();
                    if (enumerable.Any()) {
                        foreach (string import in enumerable) {
                            if (!import.Contains(Uri.SchemeDelimiter)) {
                                string filePath = HostingEnvironment.MapPath(VirtualPathUtility.Combine(dotLessPathResolver.CurrentFileDirectory, import));
                                bundler.AddFileMonitor(filePath);
                            }
                        }
                    }
                }

                return result;
            } catch (Exception ex) {
                throw ex;
            }
        }
    }
}
