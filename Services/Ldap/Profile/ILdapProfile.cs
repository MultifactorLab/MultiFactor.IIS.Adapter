namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public interface ILdapProfile
    {
        string RawUserName { get; }
        string FriendlyUserName { get; }
        string Custom2FAIdentity { get; }
        string Name { get; }
        string Email { get; }
        string Phone { get; }
    }
}