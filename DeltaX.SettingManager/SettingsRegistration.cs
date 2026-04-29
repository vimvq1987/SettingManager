namespace DeltaX.Infrastructure.Initialization
{
    // Simple wrapper to pass the type through the DI container
    public class SettingsRegistration
    {
        public Type SettingsType { get; }
        public SettingsRegistration(Type type) => SettingsType = type;
    }
}