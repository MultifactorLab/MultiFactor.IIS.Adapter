namespace MultiFactor.IIS.Adapter
{
    public class Constants
    {
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
        public const string API_USERS_QUOTA_EXCEEDED_CODE = "mfapi:0003";

        public const int DEFAULT_SESSION_LIFE_TIME_IN_HOURS = 1;
        public const int MIN_SESSION_LIFE_TIME_IN_HOURS = 1;
        public const int MAX_SESSION_LIFE_TIME_IN_HOURS = 24;

        public const int DEFAULT_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES = 5;
        public const int MIN_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES = 1;
        public const int MAX_SECOND_FACTOR_RE_REQUEST_DELAY_IN_MINUTES = 60;
    }
}