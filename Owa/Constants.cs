namespace MultiFactor.IIS.Adapter.Owa
{
    public class Constants
    {
        public const string MULTIFACTOR_PAGE = "mfa.aspx";
        public const string COOKIE_NAME = "multifactor";

        //built-in mailboxes
        public static readonly string[] SYSTEM_MAILBOX_PREFIX = new[]
        {
            "healthmailbox",
            "extest",
            "federatedemail",
            "migration",
            "systemmailbox"
        };
    }
}