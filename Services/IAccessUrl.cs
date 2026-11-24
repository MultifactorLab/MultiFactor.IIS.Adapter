using MultiFactor.IIS.Adapter.Dto;
using MultiFactor.IIS.Adapter.Services.Ldap;

namespace MultiFactor.IIS.Adapter.Services
{
    internal interface IAccessUrl
    {
        string Get(LdapIdentity identity, string postbackUrl);
        ScopeSupportInfoDto Info();
    }
}
