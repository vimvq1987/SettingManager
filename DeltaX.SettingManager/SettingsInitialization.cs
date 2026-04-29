using DeltaX.SettingManager;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using Microsoft.Extensions.DependencyInjection;

namespace DeltaX.Infrastructure.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Initialization.CmsCoreInitialization))]
    public class SettingsInitialization : IInitializableModule
    {
        public static readonly Guid SettingsRootGuid = new Guid("AAC644DF-B87A-43BE-AD5B-9FC3917C12DD");
        public static readonly Guid SettingsPageGuid = new Guid("4A21D123-5566-4B88-99AA-123456789ABC");

        public void Initialize(InitializationEngine context)
        {
            var registration = context.Locate.Advanced.GetService<SettingsRegistration>();
            if (registration == null) return; // AddSettingManager<T> was not called

            var contentRepository = context.Locate.ContentRepository();
            var contentTypeRepository = context.Locate.ContentTypeRepository();

            // 1. Ensure the Settings Root Container exists
            if (!contentRepository.TryGet(SettingsRootGuid, out SettingsContainerPage settingsRoot))
            {
                settingsRoot = contentRepository.GetDefault<SettingsContainerPage>(ContentReference.RootPage);
                settingsRoot.ContentGuid = SettingsRootGuid;
                settingsRoot.Name = "Settings Root";
                contentRepository.Save(settingsRoot, SaveAction.Publish, AccessLevel.NoAccess);
            }

            // 2. Ensure the specific Settings Page (Type T) exists
            if (!contentRepository.TryGet(SettingsPageGuid, out PageData _))
            {
                var contentType = contentTypeRepository.Load(registration.SettingsType);
                var newSettingsPage = contentRepository.GetDefault<PageData>(settingsRoot.ContentLink, contentType.ID);

                newSettingsPage.ContentGuid = SettingsPageGuid;
                newSettingsPage.Name = "Settings Page";

                contentRepository.Save(newSettingsPage, SaveAction.Publish, AccessLevel.NoAccess);
            }
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}