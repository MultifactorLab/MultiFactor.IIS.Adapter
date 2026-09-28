using MultiFactor.IIS.Adapter.Core;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class PrivacyModeDescriptorTests
    {
        [Fact]
        public void Create_FullValue_ShouldReturnFullMode()
        {
            var descriptor = PrivacyModeDescriptor.Create(" Full ");

            Assert.Equal(PrivacyMode.Full, descriptor.Mode);
        }

        [Fact]
        public void Create_PartialValue_ShouldParseFieldsCaseInsensitively()
        {
            var descriptor = PrivacyModeDescriptor.Create("Partial: Phone, Email ");

            Assert.Equal(PrivacyMode.Partial, descriptor.Mode);
            Assert.True(descriptor.HasField("phone"));
            Assert.True(descriptor.HasField("EMAIL"));
            Assert.False(descriptor.HasField("Name"));
        }

        [Fact]
        public void Create_PartialValueWithoutFields_ShouldNotHaveAnyField()
        {
            var descriptor = PrivacyModeDescriptor.Create("Partial");

            Assert.Equal(PrivacyMode.Partial, descriptor.Mode);
            Assert.False(descriptor.HasField("Name"));
            Assert.False(descriptor.HasField("Email"));
            Assert.False(descriptor.HasField("Phone"));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("Ful")]
        [InlineData("Invalid")]
        public void Create_UnexpectedValue_ShouldReturnDefault(string value)
        {
            var descriptor = PrivacyModeDescriptor.Create(value);

            Assert.Equal(PrivacyMode.None, descriptor.Mode);
        }
    }
}
