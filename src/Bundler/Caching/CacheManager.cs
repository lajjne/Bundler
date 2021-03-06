﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace Bundler.Caching {

    /// <summary>
    /// Encapsulates methods that allow the caching and retrieval of objects.
    /// </summary>
    public static class CacheManager {
        /// <summary>
        /// The cache
        /// </summary>
        private static readonly ObjectCache _cache = MemoryCache.Default;

        /// <summary>
        /// An internal list of cache keys to allow bulk removal.
        /// </summary>
        private static readonly ConcurrentDictionary<string, string> _keys = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Adds an item to the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        /// <param name="policy">
        /// Optional. An <see cref="T:System.Runtime.Caching.CacheItemPolicy"/> object that contains eviction details for the cache entry. 
        /// This object provides more options for eviction than a simple absolute expiration. The default value for the optional parameter is null.</param>
        /// <param name="regionName">
        /// Optional. A named region in the cache to which the cache entry can be added, if regions are implemented. 
        /// The default value for the optional parameter is null.
        /// </param>
        /// <returns>
        /// True if the insertion try succeeds, or false if there is an already an entry in the cache with the same key as key.
        /// </returns>
        public static bool AddItem(string key, object value, CacheItemPolicy policy = null, string regionName = null) {
            bool isAdded;

            lock (_cache) {
                if (policy == null) {
                    // create a new cache policy with the default values 
                    policy = new CacheItemPolicy();
                }

                try {
                    _cache.Set(key, value, policy, regionName);
                    isAdded = true;
                } catch {
                    isAdded = false;
                }

                if (isAdded) {
                    _keys[key] = regionName;
                }
            }

            return isAdded;
        }

        /// <summary>
        /// Fetches an item matching the given key from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="regionName">
        /// Optional. A named region in the cache to which the cache entry can be added, if regions are implemented. 
        /// The default value for the optional parameter is null.
        /// </param>
        /// <returns>The cache entry that is identified by key.</returns>
        public static object GetItem(string key, string regionName = null) {
            lock (_cache) {
                return _cache.Get(key, regionName);
            }
        }

        /// <summary>
        /// Updates an item to the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        /// <param name="policy">
        /// Optional. An <see cref="T:System.Runtime.Caching.CacheItemPolicy"/> object that contains eviction details for the cache entry. 
        /// This object provides more options for eviction than a simple absolute expiration. The default value for the optional parameter is null.
        /// </param>
        /// <param name="regionName">
        /// Optional. A named region in the cache to which the cache entry can be added, if regions are implemented. 
        /// The default value for the optional parameter is null.
        /// </param>
        /// <returns>
        /// True if the update try succeeds, or false if there is an already an entry in the cache with the same key as key.
        /// </returns>
        public static bool UpdateItem(string key, object value, CacheItemPolicy policy = null, string regionName = null) {
            bool isUpDated = true;

            // remove the item from the cache if it already exists, memoryCache will not add an item with an existing name
            if (GetItem(key, regionName) != null) {
                isUpDated = RemoveItem(key, regionName);
            }

            if (policy == null) {
                // create a new cache policy with the default values 
                policy = new CacheItemPolicy();
            }

            if (isUpDated) {
                isUpDated = AddItem(key, value, policy, regionName);
            }

            return isUpDated;
        }

        /// <summary>
        /// Removes an item matching the given key from the cache.
        /// </summary>
        /// <param name="key">A unique identifier for the cache entry.</param>
        /// <param name="regionName">
        /// Optional. A named region in the cache to which the cache entry can be added, if regions are implemented. 
        /// The default value for the optional parameter is null.
        /// </param>
        /// <returns>
        /// True if the removal try succeeds, or false if there is an already an entry in the cache with the same key as key.
        /// </returns>
        public static bool RemoveItem(string key, string regionName = null) {
            bool isRemoved;

            lock (_cache) {
                isRemoved = _cache.Remove(key, regionName) != null;

                if (isRemoved) {
                    string removedValue;
                    _keys.TryRemove(key, out removedValue);
                }
            }

            return isRemoved;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        /// <param name="regionName">The region name.</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public static bool Clear(string regionName = null) {
            bool isCleared = false;

            lock (_keys) {
                // you can't remove items from a collection whilst you are iterating over it so you need to  create a collection to store the items to remove.
                ConcurrentDictionary<string, string> tempDictionary = new ConcurrentDictionary<string, string>();

                foreach (KeyValuePair<string, string> cacheItem in _keys) {
                    // does the cached key come with a region
                    if ((cacheItem.Value == null) || (cacheItem.Value != null && cacheItem.Value.Equals(regionName, StringComparison.OrdinalIgnoreCase))) {
                        isCleared = RemoveItem(cacheItem.Key, cacheItem.Value);

                        if (isCleared) {
                            string key = cacheItem.Key;
                            string value = cacheItem.Value;
                            tempDictionary.AddOrUpdate(key, value, (oldkey, oldValue) => value);
                        }
                    }
                }

                if (isCleared) {
                    // loop through and clear out the dictionary of cache keys
                    foreach (KeyValuePair<string, string> cacheItem in tempDictionary) {
                        string removedValue;
                        _keys.TryRemove(cacheItem.Key, out removedValue);
                    }
                }
            }

            return isCleared;
        }
    }
}
