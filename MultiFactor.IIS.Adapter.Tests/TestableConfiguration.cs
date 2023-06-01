namespace MultiFactor.IIS.Adapter.Tests
{
    internal class TestableConfiguration : Configuration
    {
        public static Configuration Reload() => Load();
    }
}
