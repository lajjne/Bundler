using NUglify;
using NUglify.Css;

namespace Bundler.Compression {

    /// <summary>
    /// Helper class for performing minification of CSS Stylesheets.
    /// </summary>
    public sealed class CssMinifier {

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Bundler.Compression.CssMinifier">CssMinifier</see> class. 
        /// </summary>
        public CssMinifier() {
            RemoveWhiteSpace = true;
            ColorNames = CssColor.Strict;
        }

        /// <summary>
        /// Gets or sets what range of colors the css stylesheet should utilize.
        /// </summary>
        /// <value>What range of colors the css stylesheet should utilize</value>
        public CssColor ColorNames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whitespace should be removed from the stylesheet.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if whitespace should be removed from the stylesheet; otherwise, <see langword="false"/>.
        /// </value>
        public bool RemoveWhiteSpace { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance of the minifier should perform the minification.
        /// </summary>
        /// <value><see langword="true"/> if the minifier should perform the minification; otherwise, <see langword="false"/>.</value>
        private bool ShouldMinify => RemoveWhiteSpace;

        /// <summary>
        /// Gets the minified version of the submitted stylesheet.
        /// </summary>
        /// <param name="styleSheet">The stylesheet to minify.</param>
        /// <returns>The minified version of the submitted stylesheet.</returns>
        public string Minify(string styleSheet) {
            if (ShouldMinify) {
                if (string.IsNullOrWhiteSpace(styleSheet)) {
                    return string.Empty;
                }

                // the minifier is double escaping '\' when it finds it in the file.
                return Uglify.Css(styleSheet, CreateCssSettings()).Code.Replace(@"\5c\2e", @"\.");
            }

            return styleSheet;
        }

        /// <summary>
        /// Builds the required CssSettings class needed for the Ajax Minifier.
        /// </summary>
        /// <returns>The required CssSettings class needed for the Ajax Minifier.</returns>
        private CssSettings CreateCssSettings() {
            CssSettings cssSettings = new CssSettings {
                OutputMode = RemoveWhiteSpace ? OutputMode.SingleLine : OutputMode.MultipleLines
            };

            if (ShouldMinify) {
                // color names
                cssSettings.ColorNames = ColorNames;

                // do not keep any comments
                cssSettings.CommentMode = CssComment.None;
            }

            return cssSettings;
        }
    }
}
