using dotless.Core;
using dotless.Core.configuration;
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
    /// Provides methods to convert LESS into CSS.
    /// </summary>
    public class LessPreprocessor : IPreprocessor {

        /// <summary>
        /// Gets the extension that this filter processes.
        /// </summary>
        public string[] AllowedExtensions => new[] { ".less" };

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
            // NOTE: ScriptProcessor minifies the compiled css if needed
            ILessEngine lessEngine = new LessEngine(parser) { Compress = false };

            try {
                string result = lessEngine.TransformToCss(input, path);
                if (bundler.Options.WatchFiles) {
                    // Add each import as a file dependency
                    IEnumerable<string> imports = lessEngine.GetImports();
                    IList<string> enumerable = imports as IList<string> ?? imports.ToList();
                    if (enumerable.Any()) {
                        foreach (string import in enumerable) {
                            if (!import.Contains(Uri.SchemeDelimiter)) {
                                string filePath = HostingEnvironment.MapPath(VirtualPathUtility.Combine(dotLessPathResolver.CurrentFileDirectory, import));
                                bundler.AddFileMonitor(filePath, "not empty");
                            }
                        }
                    }
                }

                return result;
            } catch (Exception ex) {
                throw new LessCompilingException(ex.Message, ex.InnerException);
            }
        }
    }
}
