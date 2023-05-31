using System;

namespace MultiFactor.IIS.Adapter.Core
{
    public class FullyQualifiedDomainName
    {
        public string Value { get; }

        public FullyQualifiedDomainName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace.", nameof(value));
            }

            Value = value;
        }

        public override string ToString() => Value;
    }
}