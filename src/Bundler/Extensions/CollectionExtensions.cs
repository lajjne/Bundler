using Bundler.Helpers;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace Bundler.Extensions {

    /// <summary>
    /// Helper and extension methods for collections.
    /// </summary>
    public static class CollectionExtensions {

        /// <summary>
        /// Creates a collection of keys and values from the specified input object.
        /// </summary>
        /// <param name="input">Anonymous object describing HTML attributes.</param>
        /// <returns>A collection that represents HTML attributes.</returns>
        public static NameValueCollection ToNameValueCollection(this object input) {
            if (input == null) {
                return null;
            }

            NameValueCollection dict = new NameValueCollection();
            PropertyHelper[] properties = PropertyHelper.GetProperties(input);
            for (int i = 0; i < (int)properties.Length; i++) {
                PropertyHelper propertyHelper = properties[i];
                dict.Add(propertyHelper.Name, propertyHelper.GetValue(input).ToString());
            }

            return dict;
        }

        /// <summary>
        /// Converts a NameValueCollection to a html attribute string.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dashKeys"><c>true</c> to replace underscores '_' in attribute names with dashes '-'.</param>
        /// <param name="lowerKeys"><c>true</c> to render all attribute names in lower case.</param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <returns>A string, or <c>null</c> if the input list is null or empty.</returns>
        public static string AsHtmlAttributes(this NameValueCollection input, bool dashKeys = true, bool lowerKeys = false, string prefix = "", string suffix = "") {
            if (input == null || input.Count == 0) {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < input.Count; i++) {
                string key = input.GetKey(i);
                if (key == null) {
                    return null;
                }

                string outKey = key;
                if (lowerKeys) {
                    outKey = outKey.ToLower();
                }

                if (dashKeys) {
                    outKey = outKey.Replace('_', '-');
                }
                string outValue;
                string[] values = input.GetValues(i);
                int valueCount = (values != null) ? values.Length : 0;

                

                if (valueCount > 0 && builder.Length > 0) {
                    builder.Append(' ');
                }

                if (valueCount == 1) {
                    builder.Append(outKey);

                    // do not render null or empty attribute values
                    if (values[0] != null && values[0].Length > 0) {
                        builder.Append(@"=""");
                        outValue = HttpUtility.HtmlAttributeEncode(values[0]);
                        builder.Append(outValue);
                        builder.Append(@"""");
                    }
                } else if (valueCount > 1) {
                    builder.Append(@"=""");
                    for (int j = 0; j < valueCount; j++) {
                        if (j > 0) {
                            builder.Append(' ');
                        }
                        outValue = HttpUtility.HtmlAttributeEncode(values[j]);
                        builder.Append(outValue);
                    }
                    builder.Append(@"""");
                }
            }

            if (builder.Length > 0) {
                builder.Insert(0, prefix);
                builder.Append(suffix);
                return builder.ToString();
            }

            return null;
        }
    }
}
