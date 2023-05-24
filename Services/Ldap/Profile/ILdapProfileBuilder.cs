namespace MultiFactor.IIS.Adapter.Services.Ldap.Profile
{
    public interface ILdapProfileBuilder
    {
        ILdapProfileBuilder SetPhone(string value, string attrName);
        ILdapProfileBuilder SetUpn(string value);
        ILdapProfile Build();
    }
}