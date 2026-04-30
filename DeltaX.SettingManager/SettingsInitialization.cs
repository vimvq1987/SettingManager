using System;
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
            if (registration == null) return;

            var contentRepository = context.Locate.ContentRepository();
            var contentTypeRepository = context.Locate.ContentTypeRepository();
            var securityRepository = context.Locate.Advanced.GetInstance<IContentSecurityRepository>();

            // 1. Ensure the Settings Root Container exists
            if (!contentRepository.TryGet(SettingsRootGuid, out SettingsContainerPage settingsRoot))
            {
                settingsRoot = contentRepository.GetDefault<SettingsContainerPage>(ContentReference.RootPage);
                settingsRoot.ContentGuid = SettingsRootGuid;
                settingsRoot.Name = "Settings Root";

                var rootReference = contentRepository.Save(settingsRoot, SaveAction.Publish, AccessLevel.NoAccess);

                // Set ACL for Root
                SetAdminOnlyAccess(rootReference, securityRepository);
            }

            // 2. Ensure the specific Settings Page exists
            if (!contentRepository.TryGet(SettingsPageGuid, out PageData settingsPage))
            {
                var contentType = contentTypeRepository.Load(registration.SettingsType);
                var newSettingsPage = contentRepository.GetDefault<PageData>(settingsRoot.ContentLink, contentType.ID);

                newSettingsPage.ContentGuid = SettingsPageGuid;
                newSettingsPage.Name = "Settings Page";

                var pageReference = contentRepository.Save(newSettingsPage, SaveAction.Publish, AccessLevel.NoAccess);

                // Set ACL for Page (though it would inherit from Root, explicit setting ensures security)
                SetAdminOnlyAccess(pageReference, securityRepository);
            }
        }

        /// <summary>
        /// Clears existing permissions and grants full access ONLY to Administrators and WebAdmins.
        /// </summary>
        private void SetAdminOnlyAccess(ContentReference contentLink, IContentSecurityRepository securityRepository)
        {
            var securityDescriptor = securityRepository.Get(contentLink).CreateWritableClone() as IContentSecurityDescriptor;

            if (securityDescriptor != null)
            {
                // 1. Break inheritance from the parent
                securityDescriptor.IsInherited = false;

                // 2. Clear any existing entries to start fresh
                securityDescriptor.Clear();

                // 3. Add Admin roles with Full Access
                // "WebAdmins" and "Administrators" are the standard Optimizely admin roles
                var adminEntries = new[] { "WebAdmins", "Administrators" };

                foreach (var role in adminEntries)
                {
                    securityDescriptor.AddEntry(new AccessControlEntry(role, AccessLevel.FullAccess));
                }

                // 4. Save the changes
                securityRepository.Save(contentLink, securityDescriptor, SecuritySaveType.Replace);
            }
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}