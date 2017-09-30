using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bundler.Helpers;

namespace Bundler.Postprocessors.AutoPrefixer {

    /// <summary>
    /// Country statistics service
    /// </summary>
    public sealed class CountryStatisticsService {
        /// <summary>
        /// Name of directory, which contains Autoprefixer country statistics
        /// </summary>
        private const string AUTOPREFIXER_COUNTRY_STATISTICS_DIRECTORY_NAME = ".Resources.CountryStatistics.";

        /// <summary>
        /// Set of country codes for which there are statistics
        /// </summary>
        private readonly ISet<string> _countryCodes;

        /// <summary>
        /// Instance of country statistics service
        /// </summary>
        private static readonly Lazy<CountryStatisticsService> _instance =
            new Lazy<CountryStatisticsService>(() => new CountryStatisticsService());

        /// <summary>
        /// Gets a instance of country statistics service
        /// </summary>
        public static CountryStatisticsService Instance => _instance.Value;

        /// <summary>
        /// Constructs a instance of country statistics service
        /// </summary>
        private CountryStatisticsService() {
            var type = GetType();
            string[] allResourceNames = type.Assembly.GetManifestResourceNames();
            string countryResourcePrefix = type.Namespace + AUTOPREFIXER_COUNTRY_STATISTICS_DIRECTORY_NAME;
            int countryResourcePrefixLength = countryResourcePrefix.Length;
            string[] countryCodes = allResourceNames
                .Where(r => r.StartsWith(countryResourcePrefix, StringComparison.Ordinal))
                .Select(r => Path.GetFileNameWithoutExtension(r.Substring(countryResourcePrefixLength)))
                .ToArray();
            _countryCodes = new HashSet<string>(countryCodes);
        }

        /// <summary>
        /// Gets a statistics for country
        /// </summary>
        /// <param name="countryCode">Two-letter country code</param>
        /// <returns>Statistics for country</returns>
        public string GetStatisticsForCountry(string countryCode) {
            try {
                var type = GetType();
                return ResourceHelper.GetResourceAsString(type.Namespace + AUTOPREFIXER_COUNTRY_STATISTICS_DIRECTORY_NAME + countryCode + ".json", type.Assembly);
            } catch (NullReferenceException) {
                throw new AutoPrefixerProcessingException($"Could not find the statistics for country code '{countryCode}'");
            }
        }

        /// <summary>
        /// Determines whether the statistics database contains the specified country
        /// </summary>
        /// <param name="countryCode">Two-letter country code</param>
        /// <returns>true if the statistics database contains an country with the specified code;
        /// otherwise, false</returns>
        public bool ContainsCountry(string countryCode) {
            return _countryCodes.Contains(countryCode);
        }

    }
}