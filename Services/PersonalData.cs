using MultiFactor.IIS.Adapter.Core;

namespace MultiFactor.IIS.Adapter.Services
{
    public class PersonalData
    {
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string Phone { get; private set; }

        public PersonalData(string name, string email, string phone, PrivacyModeDescriptor privacyModeDescriptor)
        {
            Name = name;
            Email = email;
            Phone = phone;

            switch (privacyModeDescriptor.Mode)
            {
                case PrivacyMode.Full:
                    Name = null;
                    Email = null;
                    Phone = null;
                    break;

                case PrivacyMode.Partial:
                    if (!privacyModeDescriptor.HasField(nameof(Name)))
                    {
                        Name = null;
                    }

                    if (!privacyModeDescriptor.HasField(nameof(Email)))
                    {
                        Email = null;
                    }

                    if (!privacyModeDescriptor.HasField(nameof(Phone)))
                    {
                        Phone = null;
                    }
                    break;
            }
        }
    }
}
