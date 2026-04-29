using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace DeltaX.SettingManager
{
    public abstract class SettingsPage : PageData
    {

    }

    // Concrete class to serve as the root node
    [ContentType(DisplayName = "Settings Root", GUID = "12345678-1234-1234-1234-123456789012")]
    [AvailableContentTypes(
        Include = new[] { typeof(SettingsPage) },
        IncludeOn = new[] { typeof(PageData) })] // Can be placed under any page, or restrict to Root
    public class SettingsContainerPage : SettingsPage
    {
    }
}

namespace DeltaX.Infrastructure.Initialization
{
}
