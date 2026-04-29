using DeltaX.Infrastructure.Initialization;
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
        private readonly IPermanentLinkMapper _permanentLinkMapper;
        private readonly IContentLoader _contentLoader;
        private readonly IContentCacheKeyCreator _contentCacheKeyCreator;
        private readonly IContentEvents _contentEvents;

        private readonly ContentReference _settingPageContentLink;
        private const string CacheKey = "Settings:AllValues";

        public SettingManager(
            ISynchronizedObjectInstanceCache cache,
            IPermanentLinkMapper permanentLinkMapper,
            IContentLoader contentLoader,
            IContentCacheKeyCreator contentCacheKeyCreator,
            IContentEvents contentEvents)
        {
            _cache = cache;
            _permanentLinkMapper = permanentLinkMapper;
            _contentLoader = contentLoader;
            _contentCacheKeyCreator = contentCacheKeyCreator;
            _contentEvents = contentEvents;

            var map = _permanentLinkMapper.Find(SettingsInitialization.SettingsPageGuid);
            _settingPageContentLink = map?.ContentReference ?? ContentReference.EmptyReference;

            // Subscribe to the event to clear the cache when the page is published
            _contentEvents.PublishedContent += ContentEvents_PublishedContent;
        }

        private void ContentEvents_PublishedContent(object? sender, ContentEventArgs e)
        {
            if (e.Content is SettingsPage)
            {
                // This call triggers the remote invalidation signal to all other instances
                _cache.Remove(CacheKey);
            }
        }

        public T? GetSetting<T>(string name)
        {
            // We fetch from the Synchronized Cache every time. 
            // This is extremely fast (local memory lookup) but stays in sync across instances.
            var settings = _cache.ReadThrough(
                CacheKey,
                () => LoadFromDatabase(),
                _ => new CacheEvictionPolicy(
                    TimeSpan.FromMinutes(30),
                    CacheTimeoutType.Sliding,
                    new[] { _contentCacheKeyCreator.CreateCommonCacheKey(_settingPageContentLink) }),
                ReadStrategy.Wait // <--- This provides the "Lock on loading" behavior
            );

            if (settings != null && settings.TryGetValue(name, out var value))
            {
                return (T)value;
            }

            return default;
        }

        private IDictionary<string, object> LoadFromDatabase()
        {
            if (ContentReference.IsNullOrEmpty(_settingPageContentLink))
                return new Dictionary<string, object>();

            var page = _contentLoader.Get<PageData>(_settingPageContentLink);
            return page.Property.ToDictionary(
                x => x.Name,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}