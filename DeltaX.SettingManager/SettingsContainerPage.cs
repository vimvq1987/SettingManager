using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace DeltaX.SettingManager
{
    // Concrete class to serve as the root node
    [ContentType(DisplayName = "Settings Root", GUID = "978BA9AC-B17C-4F0F-BDE8-43FE4C2122ED")]
    [AvailableContentTypes(
        Include = new[] { typeof(SettingsPage) })] // Can be placed under any page, or restrict to Root
    public class SettingsContainerPage : PageData
    {
    }
}

