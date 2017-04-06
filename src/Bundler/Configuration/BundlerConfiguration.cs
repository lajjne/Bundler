using Bundler.Postprocessors.AutoPrefixer;
using JavaScriptEngineSwitcher.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bundler.Configuration {

    /// <summary>
    /// Encapsulates methods to allow the retrieval of cruncher settings.
    /// <see href="http://csharpindepth.com/Articles/General/Singleton.aspx"/> 
    /// </summary>
    public class BundlerConfiguration {
        /// <summary>
        /// A new instance Initializes a new instance of the <see cref="BundlerConfiguration"/> class.
        /// initialized lazily.
        /// </summary>
        private static readonly Lazy<BundlerConfiguration> Lazy = new Lazy<BundlerConfiguration>(() => new BundlerConfiguration());

        /// <summary>
        /// Represents a CruncherProcessingSection within a configuration file.
        /// </summary>
        private BundlerProcessingSection processingSection;

        /// <summary>
        /// The auto prefixer options.
        /// </summary>
        private AutoPrefixerOptions autoPrefixerOptions;

        /// <summary>
        /// Delegate that creates an instance of JavaScript engine
        /// </summary>
        private Func<IJsEngine> javaScriptEngineFunc;

        /// <summary>
        /// Prevents a default instance of the <see cref="BundlerConfiguration"/> class from being created.
        /// </summary>
        private BundlerConfiguration() {
        }

        /// <summary>
        /// Gets the current instance of the <see cref="BundlerConfiguration"/> class.
        /// </summary>
        public static BundlerConfiguration Instance {
            get {
                return Lazy.Value;
            }
        }

        /// <summary>
        /// Gets the delegate that creates an instance of JavaScript engine.
        /// </summary>
        public Func<IJsEngine> JsEngineFunc {
            get {
                if (this.javaScriptEngineFunc == null) {
                    string engineName = this.GetCruncherProcessingSection().JsEngine;

                    this.javaScriptEngineFunc = () => JsEngineSwitcher.Instance.CreateEngine(engineName);
                }

                return this.javaScriptEngineFunc;
            }
        }

        /// <summary>
        /// Gets the directory's path where to store physical files
        /// </summary>
        public string PhysicalFilesPath {
            get {
                return this.GetCruncherProcessingSection().PhysicalFiles.Path;
            }
        }

        /// <summary>
        /// Gets the number of days to keep physical files
        /// </summary>
        public int PhysicalFilesDaysBeforeRemoveExpired {
            get {
                return this.GetCruncherProcessingSection().PhysicalFiles.DaysBeforeRemoveExpired;
            }
        }

        /// <summary>
        /// Gets the auto prefixer options.
        /// </summary>
        public AutoPrefixerOptions AutoPrefixerOptions {
            get {
                return this.GetAutoPrefixerOptions();
            }
        }

        /// <summary>
        /// Retrieves the processing configuration section from the current application configuration. 
        /// </summary>
        /// <returns>The processing configuration section from the current application configuration. </returns>
        private BundlerProcessingSection GetCruncherProcessingSection() {
            return this.processingSection ?? (this.processingSection = BundlerProcessingSection.GetConfiguration());
        }

        /// <summary>
        /// Retrieves the auto prefixer configuration options from the current application configuration. 
        /// </summary>
        /// <returns>
        /// The <see cref="AutoPrefixerOptions"/> from the current application configuration.
        /// </returns>
        private AutoPrefixerOptions GetAutoPrefixerOptions() {
            if (this.autoPrefixerOptions != null) {
                return this.autoPrefixerOptions;
            }

            this.autoPrefixerOptions = new AutoPrefixerOptions {
                Enabled = this.GetCruncherProcessingSection().AutoPrefixer.Enabled,
                Browsers = this.GetCruncherProcessingSection().AutoPrefixer.Browsers.Split(',').Select(p => HttpUtility.HtmlDecode(p.Trim())).ToList(),
                Cascade = this.GetCruncherProcessingSection().AutoPrefixer.Cascade,
                Add = true,
                Remove = true,
                Supports = true,
                Flexbox = true,
                Grid = true
            };

            return this.autoPrefixerOptions;
        }
    }
}
