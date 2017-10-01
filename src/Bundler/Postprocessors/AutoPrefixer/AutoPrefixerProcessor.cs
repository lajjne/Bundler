using Bundler.Helpers;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Web;

namespace Bundler.Postprocessors.AutoPrefixer {

    /// <summary>
    /// The auto prefixer processor, <see href="https://github.com/Taritsyn/BundleTransformer/tree/master/src/BundleTransformer.Autoprefixer"/>.
    /// To update resources, download and build https://github.com/Taritsyn/BundleTransformer. Then simply copy the resource files.
    /// </summary>
    internal sealed class AutoPrefixerProcessor : IDisposable {
        /// <summary>
        /// Name of resource, which contains a AutoPrefixer library
        /// </summary>
        private const string AutoPrefixerLibraryResource = "Resources.autoprefixer-combined.min.js";

        /// <summary>
        /// Name of resource, which contains a AutoPrefixer processor helper
        /// </summary>
        private const string AutoPrefixerHelperResource = "Resources.autoprefixerHelper.min.js";

        /// <summary>
        /// Template of function call, which is responsible for compilation
        /// </summary>
        private const string CompilationFunctionCallTemplate = @"autoprefixerHelper.process({0}, {1});";

        /// <summary>
        /// Name of variable, which contains a country statistics service
        /// </summary>
        private const string COUNTRY_STATISTICS_SERVICE_VARIABLE_NAME = nameof(CountryStatisticsService);

        /// <summary>
        /// The sync root for locking against.
        /// </summary>
        private static readonly object _syncRoot = new object();

        /// <summary>
        /// The javascript engine.
        /// </summary>
        private IJsEngine _jsEngine;

        /// <summary>
        /// Whether the engine has been initialized.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// A value indicating whether this instance of the given entity has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoPrefixerProcessor"/> class.
        /// </summary>
        public AutoPrefixerProcessor() {
            // TODO: maybe pass in engine name if default engine is not what we need
            _jsEngine = JsEngineSwitcher.Instance.CreateDefaultEngine();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AutoPrefixerProcessor"/> class. 
        /// </summary>
        /// <remarks>
        /// Use C# destructor syntax for finalization code.
        /// This destructor will run only if the Dispose method 
        /// does not get called.
        /// It gives your base class the opportunity to finalize.
        /// Do not provide destructors in types derived from this class.
        /// </remarks>
        ~AutoPrefixerProcessor() {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        /// <summary>
        /// Gets a string containing the compiled CoffeeScript result.
        /// </summary>
        /// <param name="input">
        /// The input to process.
        /// </param>
        /// <param name="options">
        /// The AutoPrefixer options.
        /// </param>
        /// <returns>
        /// The <see cref="string"/> containing the compiled CoffeeScript result.
        /// </returns>
        public string Process(string input, AutoPrefixerOptions options) {
            string processedCode;

            lock (_syncRoot) {
                Initialize();

                try {
                    string result = _jsEngine.Evaluate<string>(string.Format(CompilationFunctionCallTemplate, JsonConvert.SerializeObject(input), ConvertAutoPrefixerOptionsToJson(options)));

                    JObject json = JObject.Parse(result);
                    JArray errors = json["errors"] as JArray;

                    if (errors != null && errors.Count > 0) {
                        throw new AutoPrefixerProcessingException(FormatErrorDetails(errors[0]));
                    }

                    processedCode = json.Value<string>("processedCode");
                } catch (JsRuntimeException ex) {
                    throw new AutoPrefixerProcessingException(JsErrorHelpers.Format(ex));
                }
            }

            return processedCode;
        }

        /// <summary>
        /// Disposes the object and frees resources for the Garbage Collector.
        /// </summary>
        public void Dispose() {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue 
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Converts <see cref="AutoPrefixerOptions"/> to JSON
        /// </summary>
        /// <param name="options">AutoPrefixerOptions options</param>
        /// <returns>AutoPrefixerOptions options in JSON format</returns>
        private static JObject ConvertAutoPrefixerOptionsToJson(AutoPrefixerOptions options) {
            JObject optionsJson = new JObject(
                new JProperty("browsers", new JArray(options.Browsers)),
                new JProperty("cascade", options.Cascade),
                new JProperty("add", options.Add),
                new JProperty("remove", options.Remove),
                new JProperty("supports", options.Supports),
                new JProperty("flexbox", options.Flexbox),
                new JProperty("grid", options.Grid),
                new JProperty("stats", GetCustomStatisticsFromFile(options.Stats))
            );
            return optionsJson;
        }

        /// <summary>
        /// Gets a custom statistics from specified file
        /// </summary>
        /// <param name="path">Virtual path to file, that contains custom statistics.</param>
        /// <returns>Custom statistics in JSON format</returns>
        private static JObject GetCustomStatisticsFromFile(string path) {
            if (path == null) {
                return null;
            }

            path = ResourceHelper.GetFilePath(path, null, HttpContext.Current);
            if (!File.Exists(path)) {
                throw new FileNotFoundException("Custom usage statistics not found.", path);
            }

            return JObject.Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// Generates a detailed error message.
        /// </summary>
        /// <param name="errorDetails">Error details</param>
        /// <returns>Detailed error message</returns>
        private static string FormatErrorDetails(JToken errorDetails) {
            string message = errorDetails.Value<string>("message");
            int lineNumber = errorDetails.Value<int>("lineNumber");
            int columnNumber = errorDetails.Value<int>("columnNumber");

            StringBuilder errorMessage = new StringBuilder();
            errorMessage.AppendFormat("{0}: {1}", "Message", message);
            errorMessage.AppendLine();

            if (lineNumber > 0) {
                errorMessage.AppendFormat("{0}: {1}", "Line Number", lineNumber);
                errorMessage.AppendLine();
            }

            if (columnNumber > 0) {
                errorMessage.AppendFormat("{0}: {1}", "Column Number", columnNumber);
            }

            return errorMessage.ToString();
        }

        /// <summary>
        /// Disposes the object and frees resources for the Garbage Collector.
        /// </summary>
        /// <param name="disposing">If true, the object gets disposed.</param>
        private void Dispose(bool disposing) {
            if (_disposed) {
                return;
            }

            if (disposing) {
                if (_jsEngine != null) {
                    _jsEngine.RemoveVariable(COUNTRY_STATISTICS_SERVICE_VARIABLE_NAME);
                    _jsEngine.Dispose();
                    _jsEngine = null;
                }
            }

            // Call the appropriate methods to clean up
            // unmanaged resources here.
            // Note disposing is done.
            _disposed = true;
        }

        /// <summary>
        /// Initializes CSS autoprefixer
        /// </summary>
        private void Initialize() {
            if (!_initialized) {

                _jsEngine.EmbedHostObject(COUNTRY_STATISTICS_SERVICE_VARIABLE_NAME, CountryStatisticsService.Instance);

                Type type = GetType();

                _jsEngine.ExecuteResource(AutoPrefixerLibraryResource, type);
                _jsEngine.ExecuteResource(AutoPrefixerHelperResource, type);

                _initialized = true;
            }
        }
    }
}
