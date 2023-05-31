namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public interface ILdapProfile
    {
        string SamAccountName { get; }
        string UserPrincipalName { get; }
        string Phone { get; }
        string this[string key] { get; }
    }
}