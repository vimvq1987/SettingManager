using DeltaX.SettingManager;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace DeltaX.Infrastructure.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class SettingsInitialization : IInitializableModule
    {
        private const string ContainerName = "Settings Root";
        private const string PageName = "Settings Page";

        public void Initialize(InitializationEngine context)
        {
            var registration = context.Locate.Advanced.GetService<SettingsRegistration>();
            if (registration == null) return;

            var contentRepository = context.Locate.ContentRepository();
            var contentTypeRepository = context.Locate.ContentTypeRepository();
            var securityRepository = context.Locate.Advanced.GetInstance<IContentSecurityRepository>();
            var siteRepository = context.Locate.Advanced.GetInstance<ISiteDefinitionRepository>();

            // Iterate through every site defined in the CMS
            foreach (var site in siteRepository.List())
            {
                if (ContentReference.IsNullOrEmpty(site.StartPage)) continue;

                // 1. Ensure the Settings Root Container exists under this specific site root
                var settingsRoot = contentRepository.GetChildren<SettingsContainerPage>(site.StartPage)
                    .FirstOrDefault(x => x.Name.Equals(ContainerName, StringComparison.OrdinalIgnoreCase));

                if (settingsRoot == null)
                {
                    settingsRoot = contentRepository.GetDefault<SettingsContainerPage>(site.StartPage);
                    settingsRoot.Name = ContainerName;

                    var rootReference = contentRepository.Save(settingsRoot, SaveAction.Publish, AccessLevel.NoAccess);

                    // Set ACL for Root (Admin Only)
                    SetAdminOnlyAccess(rootReference, securityRepository);

                    // Reload to get the proper reference for children
                    settingsRoot = contentRepository.Get<SettingsContainerPage>(rootReference);
                }

                // 2. Ensure the specific Settings Page exists under the container
                var settingsPage = contentRepository.GetChildren<PageData>(settingsRoot.ContentLink)
                    .FirstOrDefault(x => x.ContentTypeID == contentTypeRepository.Load(registration.SettingsType).ID);

                if (settingsPage == null)
                {
                    var contentType = contentTypeRepository.Load(registration.SettingsType);
                    var newSettingsPage = contentRepository.GetDefault<PageData>(settingsRoot.ContentLink, contentType.ID);

                    newSettingsPage.Name = PageName;

                    var pageReference = contentRepository.Save(newSettingsPage, SaveAction.Publish, AccessLevel.NoAccess);

                    // Set ACL for Page (Admin Only)
                    SetAdminOnlyAccess(pageReference, securityRepository);
                }
            }
        }

        /// <summary>
        /// Breaks inheritance and grants Full Access only to Administrators and WebAdmins.
        /// </summary>
        private void SetAdminOnlyAccess(ContentReference contentLink, IContentSecurityRepository securityRepository)
        {
            var securityDescriptor = securityRepository.Get(contentLink).CreateWritableClone() as IContentSecurityDescriptor;

            if (securityDescriptor != null)
            {
                securityDescriptor.IsInherited = false;
                securityDescriptor.Clear();

                var adminEntries = new[] { "WebAdmins", "Administrators" };

                foreach (var role in adminEntries)
                {
                    securityDescriptor.AddEntry(new AccessControlEntry(role, AccessLevel.FullAccess));
                }

                securityRepository.Save(contentLink, securityDescriptor, SecuritySaveType.Replace);
            }
        }

        public void Uninitialize(InitializationEngine context) { }
    }
}