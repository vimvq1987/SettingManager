# Optimizing Settings Management in Optimizely CMS

When running a .NET application, there have traditionally been a few ways to store settings, such as via `appSettings.json` or the **Azure Portal AppService Configuration**. 

However, these setting changes usually require a restart (or even a re-deployment), which is not always desirable or affordable in a production environment.

## The Content-Based Approach
To support changing settings on the fly, some customers have relied on a "content approach"—creating a special page containing settings as properties. This is an elegant solution because:
* **User Interface:** You have a native UI to manage values.
* **Access Control:** you can set access rights for who can view or change settings.
* **Distribution:** Once you publish the page, values are updated across all instances.

### The Caveats
Reading settings is typically a **very hot path**. If you rely solely on the standard Content API to fetch the page and its properties, you risk:
1. **Performance Bottlenecks:** High-traffic sites may slow down.
2. **Memory Allocations:** Creating unnecessary objects on every read.
3. **Threading Issues:** Potential infinite loops or race conditions when multiple threads try to read and write data to the cache simultaneously.

---

## Introducing SettingManager
To address these problems, I have created a new library as a proof of concept (POC) for leveraging the Content system for settings while avoiding performance pitfalls.

**GitHub Repository:** [vimvq1987/SettingManager](https://github.com/vimvq1987/SettingManager)

*I will eventually submit the NuGet package to the Optimizely feed, but you can start using it today by downloading and building the source.*

### Implementation

1. **Define your content type**
Your settings page should inherit from `SettingsPage`:

```csharp
[ContentType(
    DisplayName = "Setting page", 
    GUID = "452d1812-7385-42c3-8073-c1b7481e7b22", 
    Description = "", 
    AvailableInEditMode = true)]
public class MySettingPage : SettingsPage
{
    public virtual string ASetting { get; set; }
}
```

2. **Register the type**
Register the type in your `Startup.cs`:

```csharp
services.AddSettingPage<MySettingsPage>();
```

3. **Retrieve settings**
Use the `SettingsManager` to fetch settings on the fly:

```csharp
var setting = _settingManager.GetSetting<string>("ASetting");
```

---

## Key Features
* **Site-Specific Settings:** A setting page is created per site definition. You can retrieve site-specific settings; if no definition is specified, it defaults to the current site.
* **Automatic Caching:** Settings are cached and automatically cleared across all instances when a change is published.

## Feedback
As this is a POC, I am happy to receive comments, suggestions, bug reports, or fixes at the [GitHub repository](https://github.com/vimvq1987/SettingManager).
