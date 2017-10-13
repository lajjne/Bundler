using Bundler.Caching;
using Bundler.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace Bundler.Helpers {

    /// <summary>
    /// Provides a series of helper methods for dealing with resources.
    /// </summary>
    public class ResourceHelper {

        private const string CacheIdTrimPhysicalFilesFolder = "_BundlerTrimPhysicalFilesFolder";
        private const string CacheIdTrimPhysicalFilesFolderAppPoolRecycled = "_BundlerTrimPhysicalFilesFolderAppPoolRecycled";
        private const int CheckCreationDateFrequencyHours = 6;
        private const int TrimPhysicalFilesFolderDelayedExecutionMin = 5;
        private const int TrimPhysicalFilesFolderFrequencyHours = 7;

        /// <summary>
        /// Expand .bundle files.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="keepBundle"><c>true</c> to include the .bundle file in the expanded list of files, otherwise <c>false</c>.</param>
        /// <param name="rootPath">The root path to use when resolving file path for the specified resource.</param>
        /// <param name="fileNames">The list of files to expand.</param>
        /// <returns></returns>
        public static string[] ExpandBundles(HttpContext context, bool keepBundle, string rootPath, params string[] fileNames) {
            if (fileNames.Any(x => Path.GetExtension(x).Equals(Bundler.DOT_BUNDLE, StringComparison.OrdinalIgnoreCase))) {
                var monitors = new List<string>();
                var paths = new List<string>();
                string path = null;
                foreach (var fileName in fileNames) {
                    if (Path.GetExtension(fileName).Equals(Bundler.DOT_BUNDLE, StringComparison.OrdinalIgnoreCase)) {
                        path = GetFilePath(fileName, rootPath, context);
                        // watch .bundle file if applicable
                        if (BundlerSettings.Current.WatchFiles || BundlerSettings.Current.WatchAlways.Contains(path)) {
                            monitors.Add(path);
                        }

                        if (keepBundle) {
                            paths.Add(path);
                        }

                        string key = fileName.ToMd5Fingerprint() + Bundler.DOT_BUNDLE;
                        List<string> bundleFiles = CacheManager.GetItem(key) as List<string>;
                        if (bundleFiles == null) {
                            path = path ?? GetFilePath(fileName, rootPath, context);
                            if (File.Exists(path)) {
                                // add files from the .bundle file
                                bundleFiles = new List<string>();
                                var dir = Path.GetDirectoryName(path);
                                var lines = File.ReadAllLines(path);
                                string line = null;
                                int hash = -1;

                                for (int i = 0; i < lines.Length; i++) {
                                    line = lines[i].Trim();
                                    hash = line.IndexOf('#');
                                    if (hash == 0) {
                                        // lines starting with # are comments and can be skipped
                                        continue;
                                    }

                                    if (hash > 0) {
                                        // keep only part before # comment
                                        line = line.Substring(0, hash).Trim();
                                    }

                                    if (line.Length == 0) {
                                        // skip empty lines
                                        continue;
                                    }

                                    if (Path.GetExtension(line).Equals(Bundler.DOT_BUNDLE, StringComparison.OrdinalIgnoreCase)) {
                                        // resolve nested bundle
                                        var nestedBundle = GetFilePath(line, dir, context);
                                        if (BundlerSettings.Current.WatchFiles || BundlerSettings.Current.WatchAlways.Contains(nestedBundle)) {
                                            monitors.Add(nestedBundle);
                                        }

                                        bundleFiles.AddRange(ExpandBundles(context, keepBundle, Path.GetDirectoryName(nestedBundle), nestedBundle));
                                    } else {
                                        path = GetFilePath(line, dir, context);
                                        if (File.Exists(path)) {
                                            bundleFiles.Add(path);
                                        } else {
                                            throw new FileNotFoundException($"Could not locate {line} referenced in {fileName}", path);
                                        }
                                    }
                                }

                                // cache content of bundle
                                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy { Priority = CacheItemPriority.NotRemovable };
                                if (BundlerSettings.Current.WatchFiles || BundlerSettings.Current.WatchAlways.Any()) {
                                    foreach (var bf in bundleFiles) {
                                        if (BundlerSettings.Current.WatchFiles || BundlerSettings.Current.WatchAlways.Contains(bf)) {
                                            monitors.Add(bf);
                                        }
                                    }
                                    cacheItemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(monitors));
                                }
                                CacheManager.AddItem(key, bundleFiles, cacheItemPolicy);

                            } else {
                                throw new FileNotFoundException($"Could not locate {fileName}", path);
                            }
                        }

                        paths.AddRange(bundleFiles);

                    } else {
                        paths.Add(fileName);
                    }
                }
                fileNames = paths.ToArray();
            }

            return fileNames;
        }

        /// <summary>
        /// Returns the file path to the specified resource.
        /// </summary>
        /// <param name="resource">The resource to return the path for.</param>
        /// <param name="rootPath">The root path to use when resolving file path for the specified resource.</param>
        /// <param name="context">The current context.</param>
        /// <returns>
        /// The <see cref="string"/> representing the file path to the resource.
        /// </returns>
        public static string GetFilePath(string resource, string rootPath, HttpContext context) {
            try {
                // Check whether this method is invoked in an http request or not
                if (context != null) {
                    // Check whether it is a correct uri
                    if (Uri.IsWellFormedUriString(resource, UriKind.RelativeOrAbsolute)) {
                        // If the uri contains a scheme delimiter (://) then try to see if the authority is the same as the current request
                        if (resource.Contains(Uri.SchemeDelimiter)) {
                            string requestAuthority = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority);
                            if (resource.Trim().StartsWith(requestAuthority, StringComparison.CurrentCultureIgnoreCase)) {
                                string path = resource.Substring(requestAuthority.Length);
                                return HostingEnvironment.MapPath($"~{path}");
                            }

                            return resource;
                        }

                        // If it is a relative path then combine the root path with the resource's path
                        if (!Path.IsPathRooted(resource)) {
                            if (resource.StartsWith("~")) {
                                return HostingEnvironment.MapPath(resource);
                            } else {
                                return Path.GetFullPath(Path.Combine(rootPath ?? Path.GetDirectoryName(context.Request.PhysicalPath), resource));
                            }
                        }

                        // It is an absolute path
                        return HostingEnvironment.MapPath($"~{resource}");
                    }
                }

                // In any other case use the default Path.GetFullPath() method 
                return Path.GetFullPath(resource);
            } catch (Exception) {
                // If there is an error then the method returns the original resource path since the method doesn't know how to process it
                return resource;
            }
        }

        /// <summary>
        /// Gets a content of the embedded resource as string
        /// </summary>
        /// <param name="resourceName">The case-sensitive resource name</param>
        /// <param name="assembly">The assembly, which contains the embedded resource</param>
        /// <returns>Сontent of the embedded resource as string</returns>
        public static string GetResourceAsString(string resourceName, Assembly assembly) {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                if (stream == null) {
                    throw new NullReferenceException($"Resource with name '{resourceName}' is null");
                }

                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Gets or creates the specified file.
        /// </summary><param name="fileName">The file name.</param>
        /// <param name="fileContent">The file content.</param>
        /// <returns>The virtual path to the file.</returns>
        public static async Task<string> GetOrCreateFileAsync(string fileName, string fileContent) {
            var outputpath = Path.GetExtension(fileName).Equals(Bundler.DOT_JS, StringComparison.OrdinalIgnoreCase) ? BundlerSettings.Current.ScriptOutputPath : BundlerSettings.Current.StyleOutputPath;

            string fileVirtualPath = VirtualPathUtility.AppendTrailingSlash(outputpath) + fileName;
            string filePath = HostingEnvironment.MapPath(fileVirtualPath);

            // Trims the physical files folder ensuring that it does not contains files older than xx days 
            // This is performed before creating the physical resource file
            TrimFolder(HostingEnvironment.MapPath(outputpath));

            // Check whether the resource file already exists
            if (filePath != null) {

                // In order to avoid checking whether the file exists for every request (for a very busy site could be be thousands of requests per minute), we add a new cache item that will expire in a minute. 
                // That means that if the file is deleted (which should never happen) then it will be recreated after a minute.
                // With this improvement IO operations are reduced to one per minute for already existing files.
                string cacheIdCheckFileExists = $"_BundlerCheckFileExists_{fileName}";
                if (CacheManager.GetItem(cacheIdCheckFileExists) == null) {
                    CacheItemPolicy policycacheIdCheckFileExists = new CacheItemPolicy {
                        SlidingExpiration = TimeSpan.FromMinutes(1),
                        Priority = CacheItemPriority.NotRemovable
                    };
                    CacheManager.AddItem(cacheIdCheckFileExists, "1", policycacheIdCheckFileExists);

                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists) {
                        // Cache item to ensure that checking file's creation date is performed only every xx hours
                        string cacheIdCheckCreationDate = $"_BundlerCheckFileCreationDate_{fileName}";

                        // The resource file exists but it is necessary from time to time to update the file creation date in order to avoid the file to be deleted by the clean up process.
                        // To know whether the check has been performed (in order to avoid executing this check everytime) we create a cache item that will expire in 12 hours.
                        if (CacheManager.GetItem(cacheIdCheckCreationDate) == null) {
                            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);

                            CacheItemPolicy policyCheckCreationDate = new CacheItemPolicy {
                                SlidingExpiration = TimeSpan.FromHours(CheckCreationDateFrequencyHours),
                                Priority = CacheItemPriority.NotRemovable
                            };

                            CacheManager.AddItem(cacheIdCheckCreationDate, "1", policyCheckCreationDate);
                        }
                    } else {
                        // The resource file doesn't exist 
                        // Make sure that the directory exists
                        string directoryPath = HostingEnvironment.MapPath(outputpath);
                        if (directoryPath != null) {
                            DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);
                            if (!directoryInfo.Exists) {
                                // Don't swallow any errors. We want to know if this doesn't work.
                                directoryInfo.Create();
                            }
                        }

                        // Write the file asynchronously.
                        byte[] encodedText = Encoding.UTF8.GetBytes(fileContent);
                        using (
                            FileStream sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true)) {
                            await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                        }
                    }
                }
            }

            // Return the url absolute path
            return fileVirtualPath.TrimStart('~');
        }

        /// <summary>
        /// Trims the specified folder ensuring that it does not contains files older than <see cref="BundlerSettings.DaysToKeepFiles"/> days.
        /// </summary>
        /// <param name="path">The path to the folder.</param>
        public static void TrimFolder(string path) {
            // If PhysicalFilesDaysBeforeRemoveExpired is 0 or negative then the trim process is not performed
            if (BundlerSettings.Current.DaysToKeepFiles < 1) {
                return;
            }

            CacheItemPolicy policy = new CacheItemPolicy {
                Priority = CacheItemPriority.NotRemovable
            };

            // To know whether the trim process has already been performed (in order to avoid executing this process everytime) we create a cache item that will expire in 12 hours. 
            // To avoid that the cleanup process is run just after an App_Pool Recycle (or cache recycle) it uses another cache item that will never expire.
            // The main reason is because after an AppPool reset there are a lot of things going on and it is not the optimal moment to perform many I/O ops.
            if (CacheManager.GetItem(CacheIdTrimPhysicalFilesFolderAppPoolRecycled) == null) {
                // Creates the cache item that will expire first
                policy.SlidingExpiration = TimeSpan.FromMinutes(TrimPhysicalFilesFolderDelayedExecutionMin);
                CacheManager.AddItem(CacheIdTrimPhysicalFilesFolder, "1", policy);

                // Creates the cache item that will never expire
                policy.SlidingExpiration = ObjectCache.NoSlidingExpiration;
                CacheManager.AddItem(CacheIdTrimPhysicalFilesFolderAppPoolRecycled, "1", policy);

                return;
            }

            if (CacheManager.GetItem(CacheIdTrimPhysicalFilesFolder) != null) {
                return;
            }

            policy.SlidingExpiration = TimeSpan.FromHours(TrimPhysicalFilesFolderFrequencyHours);
            CacheManager.AddItem(CacheIdTrimPhysicalFilesFolder, "1", policy);

            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) {

                // Get resource files which names match Bundler's filename pattern.
                IEnumerable<FileInfo> files = new DirectoryInfo(path).EnumerateFiles();
                files = files.Where(f => f.Extension.Equals(Bundler.DOT_CSS, StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(Bundler.DOT_JS, StringComparison.OrdinalIgnoreCase)).OrderBy(f => f.CreationTimeUtc);
                int maxDays = BundlerSettings.Current.DaysToKeepFiles;

                foreach (FileInfo fileInfo in files) {
                    try {
                        // If the file's last write datetime is older that xx days then delete it
                        if (fileInfo.LastWriteTimeUtc.AddDays(maxDays) > DateTime.UtcNow) {
                            continue;
                        }

                        // Delete the file
                        fileInfo.Delete();
                    } catch {
                        // Do nothing; skip to the next file.
                    }
                }
            }
        }
    }
}
