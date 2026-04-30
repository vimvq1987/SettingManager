using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DeltaX.SettingManager
{
    public class SettingManager
    {
        private readonly ISynchronizedObjectInstanceCache _cache;
        private readonly IContentLoader _contentLoader;
        private readonly IContentCacheKeyCreator _contentCacheKeyCreator;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;
        private readonly ISiteDefinitionResolver _siteDefinitionResolver;

        private const string DataCacheKeyPrefix = "Settings:Data:";

        // Static thread-safe dictionary to store links for the lifetime of the AppDomain
        private static readonly ConcurrentDictionary<Guid, ContentReference> _localLinkCache = new();

        public SettingManager(
            ISynchronizedObjectInstanceCache cache,
            IContentLoader contentLoader,
            IContentCacheKeyCreator contentCacheKeyCreator,
            ISiteDefinitionRepository siteDefinitionRepository,
            ISiteDefinitionResolver siteDefinitionResolver)
        {
            _cache = cache;
            _contentLoader = contentLoader;
            _contentCacheKeyCreator = contentCacheKeyCreator;
            _siteDefinitionRepository = siteDefinitionRepository;
            _siteDefinitionResolver = siteDefinitionResolver;
        }

        public T? GetSetting<T>(string name, string? siteName = null)
        {
            var site = GetSite(siteName);
            if (site == null || ContentReference.IsNullOrEmpty(site.StartPage)) return default;

            // 1. Get the Link (Stored in local static memory)
            var settingsPageLink = GetLocalSettingsPageLink(site);
            if (ContentReference.IsNullOrEmpty(settingsPageLink)) return default;

            // 2. Get the Dictionary (Cached with eviction policy so data updates reflect)
            string dataCacheKey = $"{DataCacheKeyPrefix}{site.Id}";
            var settings = _cache.ReadThrough(
                dataCacheKey,
                () => LoadFromDatabase(settingsPageLink),
                _ => new CacheEvictionPolicy(
                    TimeSpan.FromMinutes(30),
                    CacheTimeoutType.Sliding,
                    new[] { _contentCacheKeyCreator.CreateCommonCacheKey(settingsPageLink) }),
                ReadStrategy.Wait
            );

            if (settings != null && settings.TryGetValue(name, out var value))
            {
                return (T)value;
            }

            return default;
        }

        private ContentReference GetLocalSettingsPageLink(SiteDefinition site)
        {
            // ConcurrentDictionary.GetOrAdd ensures the logic only runs once per Site ID
            return _localLinkCache.GetOrAdd(site.Id, _ =>
            {
                // Traverse the tree to find the page
                var container = _contentLoader.GetChildren<SettingsContainerPage>(site.StartPage)
                    .FirstOrDefault(x => x.Name.Equals("Settings Root", StringComparison.OrdinalIgnoreCase));

                if (container == null) return ContentReference.EmptyReference;

                var settingsPage = _contentLoader.GetChildren<PageData>(container.ContentLink)
                    .FirstOrDefault();

                return settingsPage?.ContentLink ?? ContentReference.EmptyReference;
            });
        }

        private SiteDefinition? GetSite(string? siteName)
        {
            if (string.IsNullOrWhiteSpace(siteName))
            {
                return _siteDefinitionResolver.GetByHostname(HostDefinition.WildcardHostName, true);
            }

            return _siteDefinitionRepository
                .List()
                .FirstOrDefault(x => x.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase));
        }

        private IDictionary<string, object> LoadFromDatabase(ContentReference contentLink)
        {
            var page = _contentLoader.Get<PageData>(contentLink);
            return page.Property.ToDictionary(
                x => x.Name,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}