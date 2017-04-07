using Bundler.Caching;
using Bundler.Configuration;
using Bundler.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;

namespace Bundler {

    /// <summary>
    /// The base processor. Asset processors inherit from this.
    /// </summary>
    public abstract class ProcessorBase {


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

        /// <summary>
        /// Expand .bundle files
        /// </summary>
        /// <param name="bundler"></param>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        protected string[] ExpandBundles(BundlerBase bundler, params string[] fileNames) {
            List<string> paths = new List<string>();
            foreach (var fileName in fileNames) {
                if (Path.GetExtension(fileName) == ".bundle") {
                    string bundleFile = ResourceHelper.GetFilePath(fileName, bundler.Options.RootFolder, bundler.Context);
                    if (File.Exists(bundleFile)) {
                        // Add the filenames from the bundle
                        var lines = File.ReadAllLines(bundleFile);
                        foreach (var line in lines) {
                            if (line.StartsWith("#")) {
                                continue;
                            }
                            var path = ResourceHelper.GetFilePath(line, Path.GetDirectoryName(bundleFile), bundler.Context);
                            if (File.Exists(path)) {
                                paths.Add(path);
                            }
                        }

                        // Monitor .bundle for changes
                        bundler.AddFileMonitor(bundleFile, string.Join(Environment.NewLine, lines));
                    }
                } else {
                    paths.Add(fileName);
                }
            }

            return paths.ToArray();
        }

    }
}
