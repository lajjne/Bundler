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
        /// The remote regex.
        /// </summary>
        private static readonly Regex RemoteRegex = new Regex(@"^http(s?)://", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="BundlerBase"/> class.
        /// </summary>
        /// <param name="options">The options containing instructions for the bundler.</param>
        protected BundlerBase(BundlerOptions options, HttpContext context) {
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
        public BundlerOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the file monitors.
        /// </summary>
        public ConcurrentBag<string> FileMonitors { get; set; }

        /// <summary>
        /// Bundles the specified resource.
        /// </summary>
        /// <param name="resource">The file or folder containing the resource(s) to bundle.</param>
        /// <returns>The bundled resource.</returns>
        public async Task<string> CrunchAsync(string resource) {
            StringBuilder stringBuilder = new StringBuilder();

            if (this.IsValidPath(resource)) {
                stringBuilder.Append(await this.LoadLocalFolderAsync(resource));
            } else {
                stringBuilder.Append(await this.LoadLocalFileAsync(resource));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Adds a cached file monitor to the list.
        /// </summary>
        /// <param name="file">
        /// The file to add to the monitors list.
        /// </param>
        /// <param name="contents">
        /// The contents of the file.
        /// </param>
        public void AddFileMonitor(string file, string contents) {
            // Cache if applicable.
            if (this.Options.CacheFiles && !string.IsNullOrWhiteSpace(contents)) {
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
        protected virtual async Task<string> LoadLocalFileAsync(string file) {
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

            input = PreprocessorManager.Instance.PreProcessors
                .Where(p => p.AllowedExtensions != null && p.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                .Aggregate(input, (current, p) => p.Transform(current, path, this));

            return input;
        }

        /// <summary>
        /// Loads the local folder.
        /// </summary>
        /// <param name="folder">The folder to load resources from.</param>
        /// <returns>The contents of the resources in the folder as a string.</returns>
        private async Task<string> LoadLocalFolderAsync(string folder) {
            StringBuilder stringBuilder = new StringBuilder();
            DirectoryInfo directoryInfo = new DirectoryInfo(folder);

            foreach (FileInfo fileInfo in await directoryInfo.EnumerateFilesAsync("*", SearchOption.AllDirectories)) {
                stringBuilder.Append(await this.LoadLocalFileAsync(fileInfo.FullName));
            }

            return stringBuilder.ToString();
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

        /// <summary>
        /// Determines whether the current resource is a valid path.
        /// </summary>
        /// <param name="resource">The file or folder containing the resource(s) to check.</param>
        /// <returns>
        ///   <c>true</c> if the current resource is a valid path; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValidPath(string resource) {
            return resource.Contains("\\") && Directory.Exists(resource);
        }



    }
}
