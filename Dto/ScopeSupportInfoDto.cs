

namespace MultiFactor.IIS.Adapter.Dto
{
    public class ScopeSupportInfoDto
    {
        public string AdminName { get; set; }
        public string AdminEmail { get; set; }
        public string AdminPhone { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrWhiteSpace(AdminName) 
                && string.IsNullOrWhiteSpace(AdminEmail) 
                && string.IsNullOrWhiteSpace(AdminPhone);
        }
    }
}