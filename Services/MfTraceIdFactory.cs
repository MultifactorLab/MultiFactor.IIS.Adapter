using System;

namespace MultiFactor.IIS.Adapter.Services
{
    public static class MfTraceIdFactory
    {
        public static string CreateTraceOwa() => $"iis-owa-{Guid.NewGuid()}";
        public static string CreateTraceCrm() => $"iis-crm-{Guid.NewGuid()}";
    }
}