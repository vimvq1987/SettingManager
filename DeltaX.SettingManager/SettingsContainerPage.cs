using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace DeltaX.SettingManager
{
    // Concrete class to serve as the root node
    [ContentType(DisplayName = "Settings Root", GUID = "12345678-1234-1234-1234-123456789012")]
    [AvailableContentTypes(
        Include = new[] { typeof(SettingsPage) })] // Can be placed under any page, or restrict to Root
    public class SettingsContainerPage : ContentFolder
    {
    }
}

