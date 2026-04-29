using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace DeltaX.SettingManager
{
    public abstract class SettingsPage : PageData
    {

    }

    [ContentType]
    public class SampleSettingsPage : SettingsPage
    {
        public virtual string SomeSetting { get; set; }
    }
}

