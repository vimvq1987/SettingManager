using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Web;
using System;
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
        private const string LinkCacheKeyPrefix = "Settings:Link:";

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

            // 1. Get the Link (Cached)
            var settingsPageLink = GetCachedSettingsPageLink(site);
            if (ContentReference.IsNullOrEmpty(settingsPageLink)) return default;

            // 2. Get the Dictionary (Cached)
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

        private ContentReference GetCachedSettingsPageLink(SiteDefinition site)
        {
            string linkCacheKey = $"{LinkCacheKeyPrefix}{site.Id}";

            return _cache.ReadThrough(linkCacheKey, () =>
            {
                // Traverse the tree only when the cache is empty
                var container = _contentLoader.GetChildren<SettingsContainerPage>(site.StartPage)
                    .FirstOrDefault(x => x.Name.Equals("Settings Root", StringComparison.OrdinalIgnoreCase));

                if (container == null) return ContentReference.EmptyReference;

                var settingsPage = _contentLoader.GetChildren<PageData>(container.ContentLink)
                    .FirstOrDefault();

                return settingsPage?.ContentLink ?? ContentReference.EmptyReference;

            }, _ => new CacheEvictionPolicy(
                // Evict this link if the StartPage changes or its children change
                new[] { _contentCacheKeyCreator.CreateCommonCacheKey(site.StartPage) }
            ));
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