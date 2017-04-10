using Bundler.Extensions;
using Bundler.Preprocessors;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Bundler {

    /// <summary>
    /// Bundler base class. Inherit from this to implement your own bundler. 
    /// </summary>
    public abstract class BundlerBase {
        /// <summary>
        /// The current context.
        /// </summary>
        private readonly HttpContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundlerBase"/> class.
        /// </summary>
        /// <param name="options">The options containing instructions for the bundler.</param>
        protected BundlerBase(BundleOptions options, HttpContext context) {
            this.context = context;
            this.Options = options;
            this.FileMonitors = new ConcurrentBag<string>();
        }

        /// <summary>
        /// Gets the current context.
        /// </summary>
        public HttpContext Context {
            get {
                return context;
            }
        }

        /// <summary>
        /// Gets or sets the options containing instructions for the bundler.
        /// </summary>
        public BundleOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the file monitors.
        /// </summary>
        public ConcurrentBag<string> FileMonitors { get; set; }

        /// <summary>
        /// Process the specified resource.
        /// </summary>
        /// <param name="resource">The file or folder containing the resource(s) to bundle.</param>
        /// <returns>The bundled resource.</returns>
        public async Task<string> ProcessAsync(string resource) {
            return await this.LoadFileAsync(resource);
        }

        /// <summary>
        /// Adds a file monitor to the list.
        /// </summary>
        /// <param name="file">The file to add to the monitors list.</param>
        public void AddFileMonitor(string file) {
            // Cache if applicable.
            if (this.Options.WatchFiles) {
                this.FileMonitors.Add(file);
            }
        }

        /// <summary>
        /// Minifies the specified resource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>The minified resource.</returns>
        public abstract string Minify(string resource);

        /// <summary>
        /// Loads the local file.
        /// </summary>
        /// <param name="file">The file to load.</param>
        /// <returns>The contents of the local file as a string.</returns>
        protected virtual async Task<string> LoadFileAsync(string file) {
            string contents = string.Empty;

            if (this.IsValidFile(file)) {
                using (StreamReader streamReader = new StreamReader(file)) {
                    contents = await streamReader.ReadToEndAsync();
                }
            }

            return contents;
        }

        /// <summary>
        /// Transforms the content of the given string using the correct PreProcessor. 
        /// </summary>
        /// <param name="input">The input string to transform.</param>
        /// <param name="path">The path to the file.</param>
        /// <returns>The transformed string.</returns>
        protected virtual string PreProcessInput(string input, string path) {
            string extension = path.Substring(path.LastIndexOf('.')).ToUpperInvariant();

            input = PreprocessorManager.Instance.Preprocessors
                .Where(p => p.AllowedExtensions != null && p.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                .Aggregate(input, (current, p) => p.Transform(current, path, this));

            return input;
        }

        /// <summary>
        /// Determines whether the current resource is a valid file.
        /// </summary>
        /// <param name="resource">The file or folder containing the resource(s) to check.</param>
        /// <returns>
        ///   <c>true</c> if the current resource is a valid file; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidFile(string resource) {
            return PreprocessorManager.Instance.AllowedExtensionsRegex.IsMatch(resource) && File.Exists(resource);
        }
    }
}
