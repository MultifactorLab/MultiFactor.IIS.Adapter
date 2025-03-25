namespace MultiFactor.IIS.Adapter.Core
{
    public class PersonalData
    {
        public string Email { get; }
        public string Phone { get; }
        public string Identity { get; }

        public PersonalData(string identity, string email, string phone)
        {
            Identity = identity;
            Email = email;
            Phone = phone;
        }
    }
}
