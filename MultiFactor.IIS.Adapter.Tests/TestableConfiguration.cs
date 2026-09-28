using System;
using System.Collections.Specialized;
using System.Reflection;

namespace MultiFactor.IIS.Adapter.Tests
{
    internal class TestableConfiguration : Configuration
    {
        private static readonly FieldInfo _currentField =
            typeof(Configuration).GetField("_current", BindingFlags.NonPublic | BindingFlags.Static);

        public static Configuration Reload() => Load();

        public static Configuration Reload(NameValueCollection appSettings) => Load(appSettings);

        public static void SetCurrentPublicUrl(string publicUrl)
        {
            var configuration = new TestableConfiguration { PublicUrl = publicUrl };
            _currentField.SetValue(null, new Lazy<Configuration>(() => configuration));
        }

        public static void ResetCurrent()
        {
            _currentField.SetValue(null, new Lazy<Configuration>(Load));
        }
    }
}
