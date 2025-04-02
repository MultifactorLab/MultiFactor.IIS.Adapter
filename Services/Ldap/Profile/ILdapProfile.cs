namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public interface ILdapProfile
    {
        string RawUserName { get; }
        string FriendlyUserName { get; }
        string Custom2FAIdentity { get; }
        string Phone { get; }
    }
}