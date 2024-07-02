namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public interface ILdapProfileBuilder
    {
        ILdapProfileBuilder SetPhone(string value, string attrName);
        ILdapProfileBuilder Set2FAIdentityAttribute(string value);
        ILdapProfile Build();
    }
}