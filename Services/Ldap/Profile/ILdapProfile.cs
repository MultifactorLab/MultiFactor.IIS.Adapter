namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public interface ILdapProfile
    {
        string SamAccountName { get; }
        string TwoFAIdentity { get; }
        string Phone { get; }
    }
}