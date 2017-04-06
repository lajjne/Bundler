using Bundler.Caching;
using Bundler.Configuration;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.Caching;

namespace Bundler {

    /// <summary>
    /// The base processor. Asset processors inherit from this.
    /// </summary>
    public abstract class ProcessorBase {
        /// <summary>
        /// Returns a Uri representing the url from the given token from the whitelist in the web.config.
        /// </summary>
        /// <param name="token">The token to look up.</param>
        /// <returns>A Uri representing the url from the given token from the whitelist in the web.config.</returns>
        protected Uri GetUrlFromToken(string token) {
            Uri url = null;
            BundlerSecuritySection.WhiteListElementCollection remoteFileWhiteList = BundlerConfiguration.Instance.RemoteFileWhiteList;
            BundlerSecuritySection.SafeUrl safeUrl = remoteFileWhiteList.Cast<BundlerSecuritySection.SafeUrl>()
                                                                         .FirstOrDefault(item => item.Token.ToUpperInvariant()
                                                                         .Equals(token.ToUpperInvariant()));

            if (safeUrl != null) {
                // Url encode any value here as we cannot store them encoded in the web.config.
                url = safeUrl.Url;
            }

            return url;
        }

        /// <summary>
        /// Adds a resource to the cache.
        /// </summary>
        /// <param name="key">
        /// The filename of the item to add.
        /// </param>
        /// <param name="contents">
        /// The contents of the file to cache.
        /// </param>
        /// <param name="fileMonitors">
        /// The file monitors to keep track of.
        /// </param>
        protected void AddItemToCache(string key, string contents, ConcurrentBag<string> fileMonitors) {
            if (!string.IsNullOrWhiteSpace(contents)) {
                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy { Priority = CacheItemPriority.NotRemovable };

                if (fileMonitors.Any()) {
                    cacheItemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(fileMonitors.ToList()));
                }

                CacheManager.AddItem(key, contents, cacheItemPolicy);
            }
        }
    }
}
