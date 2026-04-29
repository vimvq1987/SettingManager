using DeltaX.SettingManager;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace DeltaX.Infrastructure.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Initialization.CmsCoreInitialization))]
    public class SettingsInitialization : IInitializableModule
    {
        public static readonly Guid SettingsPageGuid = new Guid("4A21D123-5566-4B88-99AA-123456789ABC");

        public void Initialize(InitializationEngine context)
        {
            var contentRepository = context.Locate.ContentRepository();
            var contentTypeRepository = context.Locate.ContentTypeRepository();

            // 1. Check if the "Settings" root already exists under the system Root
            var root = ContentReference.RootPage;
            var settingsRoot = contentRepository.GetChildren<SettingsContainerPage>(root).FirstOrDefault();

            if (settingsRoot == null)
            {
                // 2. Get the ContentType ID for our container
                var contentType = contentTypeRepository.Load<SettingsContainerPage>();

                // 3. Create a new instance
                var newSettingsPage = contentRepository.GetDefault<SettingsContainerPage>(root, contentType.ID);
                newSettingsPage.ContentGuid = SettingsPageGuid;
                newSettingsPage.PageName = "Settings";

                // 4. Save and Publish
                contentRepository.Save(newSettingsPage, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);
            }
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}
