using MultiFactor.IIS.Adapter.Core;
using MultiFactor.IIS.Adapter.Services;
using Xunit;

namespace MultiFactor.IIS.Adapter.Tests
{
    public class PersonalDataTests
    {
        [Fact]
        public void None_ShouldKeepAllFields()
        {
            var descriptor = PrivacyModeDescriptor.Create("None");

            var personalData = new PersonalData("name", "email", "phone", descriptor);

            Assert.Equal("name", personalData.Name);
            Assert.Equal("email", personalData.Email);
            Assert.Equal("phone", personalData.Phone);
        }

        [Fact]
        public void Full_ShouldHideAllFields()
        {
            var descriptor = PrivacyModeDescriptor.Create("Full");

            var personalData = new PersonalData("name", "email", "phone", descriptor);

            Assert.Null(personalData.Name);
            Assert.Null(personalData.Email);
            Assert.Null(personalData.Phone);
        }

        [Theory]
        [InlineData("Name", "name", null, null)]
        [InlineData("Email", null, "email", null)]
        [InlineData("Phone", null, null, "phone")]
        [InlineData("Name,Email,Phone", "name", "email", "phone")]
        public void Partial_ShouldKeepOnlyConfiguredFields(
            string fields,
            string expectedName,
            string expectedEmail,
            string expectedPhone)
        {
            var descriptor = PrivacyModeDescriptor.Create("Partial:" + fields);

            var personalData = new PersonalData("name", "email", "phone", descriptor);

            Assert.Equal(expectedName, personalData.Name);
            Assert.Equal(expectedEmail, personalData.Email);
            Assert.Equal(expectedPhone, personalData.Phone);
        }

        [Fact]
        public void PartialWithoutFields_ShouldHideAllFields()
        {
            var descriptor = PrivacyModeDescriptor.Create("Partial");

            var personalData = new PersonalData("name", "email", "phone", descriptor);

            Assert.Null(personalData.Name);
            Assert.Null(personalData.Email);
            Assert.Null(personalData.Phone);
        }
    }
}
