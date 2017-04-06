using Bundler.Configuration;

namespace Bundler.Postprocessors.AutoPrefixer {

    /// <summary>
    /// The auto prefixer postprocessor.
    /// by using Andrey Sitnik's Autoprefixer
    /// <see href="https://bundletransformer.codeplex.com"/>
    /// </summary>
    public class AutoPrefixerPostprocessor {
        /// <summary>
        /// Transforms the content of the given string. 
        /// </summary>
        /// <param name="input">
        /// The input string to transform.
        /// </param>
        /// <param name="options">
        /// The <see cref="AutoPrefixerOptions"/>.
        /// </param>
        /// <returns>
        /// The transformed string.
        /// </returns>
        public string Transform(string input, AutoPrefixerOptions options) {
            if (!options.Enabled) {
                return input;
            }

            using (AutoPrefixerProcessor processor = new AutoPrefixerProcessor(BundlerConfiguration.Instance.JsEngineFunc)) {
                input = processor.Process(input, options);
            }

            return input;
        }
    }
}
