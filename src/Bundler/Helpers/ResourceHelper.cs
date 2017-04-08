using Bundler.Caching;
using Bundler.Configuration;
using Bundler.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;

namespace Bundler.Helpers {

    /// <summary>
    /// Provides a series of helper methods for dealing with resources.
    /// </summary>
    public class ResourceHelper {
        /// <summary>
        /// The physical file regex.
        /// </summary>
        private static readonly Regex PhysicalFileRegex = new Regex(@"\.(css|js)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Expand .bundle files
        /// </summary>
        /// <param name="bundler"></param>
        /// <param name="keepBundle"></param>
        /// <param name="fileNames"></param>
        /// <returns></returns>
        public static string[] ExpandBundles(HttpContext context, bool keepBundle, params string[] fileNames) {
            if (fileNames.Any(x => Path.GetExtension(x) == ".bundle")) {
                List<string> paths = new List<string>();
                foreach (var fileName in fileNames) {
                    if (Path.GetExtension(fileName) == ".bundle") {
                        string bundleFile = null;
                        if (keepBundle) {
                            bundleFile = ResourceHelper.GetFilePath(fileName, Path.GetDirectoryName(context.Request.FilePath), context);
                            paths.Add(bundleFile);
                        }

                        string key = fileName.ToMd5Fingerprint();
                        string[] bundleFiles = CacheManager.GetItem(key) as string[];
                        if (bundleFiles == null) {
                            bundleFile = bundleFile ?? ResourceHelper.GetFilePath(fileName, Path.GetDirectoryName(context.Request.FilePath), context);
                            if (File.Exists(bundleFile)) {
                                var dir = Path.GetDirectoryName(bundleFile);
                                // Add the filenames from the bundle
                                bundleFiles = File.ReadAllLines(bundleFile);
                                for (int i = 0; i < bundleFiles.Length; i++) {
                                    if (bundleFiles[i].StartsWith("#")) {
                                        bundleFiles[i] = null;
                                    } else {
                                        bundleFiles[i] = ResourceHelper.GetFilePath(bundleFiles[i], dir, context);
                                    }
                                }

                                // Remove null paths
                                bundleFiles = bundleFiles.Where(x => x != null).ToArray();

                                // Cache content of bundle
                                CacheItemPolicy cacheItemPolicy = new CacheItemPolicy { Priority = CacheItemPriority.NotRemovable };
                                cacheItemPolicy.ChangeMonitors.Add(new HostFileChangeMonitor(new string[] { bundleFile }));
                                CacheManager.AddItem(key, bundleFiles, cacheItemPolicy);
                            }
                        }
                        if (bundleFiles != null) {
                            paths.AddRange(bundleFiles);
                        }
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
        /// <param name="rootPath">The root path for the request.</param>
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

                        // If it is a relative path then combines the request's path with the resource's path
                        if (!Path.IsPathRooted(resource)) {
                            return Path.GetFullPath(Path.Combine(rootPath, resource));
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
                    throw new NullReferenceException(string.Format("Resource with name '{0}' is null", resourceName));
                }

                using (var reader = new StreamReader(stream)) {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// The create resource physical file.
        /// </summary>
        /// <param name="fileName">
        /// The file name.
        /// </param>
        /// <param name="fileContent">
        /// The file content.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static async Task<string> CreateResourcePhysicalFileAsync(string fileName, string fileContent) {
            // Cache item to ensure that checking file's creation date is performed only every xx hours
            string cacheIdCheckCreationDate = $"_CruncherCheckFileCreationDate_{fileName}";
            const int CheckCreationDateFrequencyHours = 6;

            // Cache item to ensure that checking whether the file exists is performed only every xx hours
            string cacheIdCheckFileExists = $"_CruncherCheckFileExists_{fileName}";

            string fileVirtualPath = VirtualPathUtility.AppendTrailingSlash(BundlerConfiguration.Instance.PhysicalFilesPath) + fileName;
            string filePath = HostingEnvironment.MapPath(fileVirtualPath);

            // Trims the physical files folder ensuring that it does not contains files older than xx days 
            // This is performed before creating the physical resource file
            await TrimPhysicalFilesFolderAsync(HostingEnvironment.MapPath(BundlerConfiguration.Instance.PhysicalFilesPath));

            // Check whether the resource file already exists
            if (filePath != null) {
                // In order to avoid checking whether the file exists for every request (for a very busy site could be be thousands of requests per minute)
                // a new cache item is added that will expire in a minute is created. That means that if the file is deleted (what should never happen) then it will be recreated after one minute.
                // With this improvement IO operations are reduced to one per minute for already existing files
                if (CacheManager.GetItem(cacheIdCheckFileExists) == null) {
                    CacheItemPolicy policycacheIdCheckFileExists = new CacheItemPolicy {
                        SlidingExpiration = TimeSpan.FromMinutes(1),
                        Priority = CacheItemPriority.NotRemovable
                    };
                    CacheManager.AddItem(cacheIdCheckFileExists, "1", policycacheIdCheckFileExists);

                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists) {
                        // The resource file exists but it is necessary from time to time to update the file creation date 
                        // in order to avoid the file to be deleted by the clean up process.
                        // To know whether the check has been performed (in order to avoid executing this check everytime) creates 
                        // a cache item that will expire in 12 hours.
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
                        string directoryPath = HostingEnvironment.MapPath(BundlerConfiguration.Instance.PhysicalFilesPath);
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
                            FileStream sourceStream = new FileStream(
                                filePath,
                                FileMode.Create,
                                FileAccess.Write,
                                FileShare.None,
                                4096,
                                true)) {
                            await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                        }
                    }
                }
            }

            // Return the url absolute path
            return fileVirtualPath.TrimStart('~');
        }

        /// <summary>
        /// Trims the physical files folder ensuring that it does not contains files older than xx days 
        /// </summary>
        /// <param name="path">
        /// The path to the folder.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public static async Task TrimPhysicalFilesFolderAsync(string path) {
            // If PhysicalFilesDaysBeforeRemoveExpired is 0 or negative then the trim process is not performed
            if (BundlerConfiguration.Instance.PhysicalFilesDaysBeforeRemoveExpired < 1) {
                return;
            }

            // Settings for the clean up process
            const string CacheIdTrimPhysicalFilesFolder = "_CruncherTrimPhysicalFilesFolder";
            const string CacheIdTrimPhysicalFilesFolderAppPoolRecycled = "_CruncherTrimPhysicalFilesFolderAppPoolRecycled";
            const int TrimPhysicalFilesFolderDelayedExecutionMin = 5;
            const int TrimPhysicalFilesFolderFrequencyHours = 7;

            CacheItemPolicy policy = new CacheItemPolicy {
                Priority = CacheItemPriority.NotRemovable
            };

            // To know whether the trim process has already been performed (in order to avoid executing this process everytime) creates 
            // a cache item that will expire in 12 hours. 
            // To avoid that the cleanup process is run just after an App_Pool Recycle (or cache recycle) it uses another cache item that will never expire
            // The main reason is because after an AppPool reset there are a lot of things going on and it is not the optimal moment to perform many I/O ops
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
                DirectoryInfo directoryInfo = new DirectoryInfo(path);

                // Regular expression to get resource files which names match Bundler's filename pattern.
                IEnumerable<FileInfo> files = await directoryInfo.EnumerateFilesAsync();
                files = files.Where(f => PhysicalFileRegex.IsMatch(Path.GetFileName(f.Name)))
                             .OrderBy(f => f.CreationTimeUtc);
                int maxDays = BundlerConfiguration.Instance.PhysicalFilesDaysBeforeRemoveExpired;

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
