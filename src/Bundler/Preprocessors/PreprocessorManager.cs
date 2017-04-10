using Bundler.Extensions;
using Bundler.Preprocessors.Css;
using Bundler.Preprocessors.Less;
using Bundler.Preprocessors.Sass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Bundler.Preprocessors {

    /// <summary>
    /// 
    /// </summary>
    public class PreprocessorManager {
        /// <summary>
        /// A new instance Initializes a new instance of the <see cref="T:Bundler.Preprocessors.PreprocessorManager"/> class.
        /// initialized lazily.
        /// </summary>
        private static readonly Lazy<PreprocessorManager> Lazy = new Lazy<PreprocessorManager>(() => new PreprocessorManager());

        /// <summary>
        /// Prevents a default instance of the <see cref="T:Bundler.Preprocessors.PreprocessorManager"/> class from being created.
        /// </summary>
        private PreprocessorManager() {
            this.LoadPreprocessors();
            this.CreateAllowedExtensionRegex();
        }

        /// <summary>
        /// Gets the current instance of the <see cref="T:Bundler.Preprocessors.PreprocessorManager"/> class.
        /// </summary>
        public static PreprocessorManager Instance => Lazy.Value;

        /// <summary>
        /// Gets the list of available Preprocessors.
        /// </summary>
        public IList<IPreprocessor> Preprocessors { get; private set; }

        /// <summary>
        /// Gets the regular expression for matching allowed file type.
        /// </summary>
        public Regex AllowedExtensionsRegex { get; private set; }

        /// <summary>
        /// Load the preprocessors that Bundler can run. We should probably use a DI container for this instead of hardcoding, but this works for now-
        /// </summary>
        private void LoadPreprocessors() {
            Preprocessors = new List<IPreprocessor>();
            Preprocessors.Add(new CssPreprocessor());
            Preprocessors.Add(new LessPreprocessor());
            Preprocessors.Add(new SassPreprocessor());
            Preprocessors.Add(new ResourcePreprocessor());
        }

        /// <summary>
        /// Generates a Regex with a list of allowed file type extensions.
        /// </summary>
        private void CreateAllowedExtensionRegex() {
            StringBuilder stringBuilder = new StringBuilder(@"\.js|");

            foreach (IPreprocessor preprocessor in this.Preprocessors) {
                string[] extensions = preprocessor.AllowedExtensions;

                if (extensions != null) {
                    foreach (string extension in extensions) {
                        stringBuilder.AppendFormat(@"\{0}|", extension.ToLowerInvariant());
                    }
                }
            }

            this.AllowedExtensionsRegex = new Regex(string.Format("({0})$", stringBuilder.ToString().TrimEnd('|')), RegexOptions.IgnoreCase);
        }
    }
}
