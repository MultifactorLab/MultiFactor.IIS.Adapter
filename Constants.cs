namespace MultiFactor.IIS.Adapter
{
    public class Constants
    {
        public const string MULTIFACTOR_PAGE = "mfa.aspx";
        public const string COOKIE_NAME = "multifactor";
        public const string RAW_USER_NAME_CLAIM = "rawUserName";

        //built-in mailboxes
        public static readonly string[] EXCHANGE_SYSTEM_MAILBOX_PREFIX = new[]
        {
            "healthmailbox",
            "extest",
            "federatedemail",
            "migration",
            "systemmailbox"
        };

        public const string API_UNREACHABLE_CODE = "mfapi:0001";
        public const string API_NOT_REGISTERED_CODE = "mfapi:0002";
    }
}