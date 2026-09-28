using System;
using System.Linq;

namespace MultiFactor.IIS.Adapter.Core
{
    public class PrivacyModeDescriptor
    {
        private readonly string[] _fields;

        public PrivacyMode Mode { get; }

        public static PrivacyModeDescriptor Default => new PrivacyModeDescriptor(PrivacyMode.None);

        private PrivacyModeDescriptor(PrivacyMode mode, params string[] fields)
        {
            Mode = mode;
            _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public static PrivacyModeDescriptor Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Default;
            }

            if (!TryGetMode(value, out var mode))
            {
                return Default;
            }

            if (mode != PrivacyMode.Partial)
            {
                return new PrivacyModeDescriptor(mode);
            }

            return new PrivacyModeDescriptor(mode, GetFields(value));
        }

        public bool HasField(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                return false;
            }

            return _fields.Any(x => x.Equals(field, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetMode(string value, out PrivacyMode mode)
        {
            var separatorIndex = value.IndexOf(':');
            var modeValue = (separatorIndex == -1 ? value : value.Substring(0, separatorIndex)).Trim();

            // Enum.TryParse also accepts numbers, but they are not a valid setting value
            var isSupportedMode = Enum.GetNames(typeof(PrivacyMode))
                .Any(x => x.Equals(modeValue, StringComparison.OrdinalIgnoreCase));

            if (!isSupportedMode)
            {
                mode = PrivacyMode.None;
                return false;
            }

            return Enum.TryParse(modeValue, true, out mode);
        }

        private static string[] GetFields(string value)
        {
            var separatorIndex = value.IndexOf(':');
            if (separatorIndex == -1 || value.Length <= separatorIndex + 1)
            {
                return Array.Empty<string>();
            }

            return value.Substring(separatorIndex + 1)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Distinct()
                .ToArray();
        }
    }
}
