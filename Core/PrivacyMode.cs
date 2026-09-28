namespace MultiFactor.IIS.Adapter.Core
{
    /// <summary>
    /// User information disclosure mode.
    /// </summary>
    public enum PrivacyMode
    {
        /// <summary>
        /// Include all personal data.
        /// </summary>
        None,

        /// <summary>
        /// Disable all personal data but identity.
        /// </summary>
        Full,

        /// <summary>
        /// Disable all personal data but identity and explicitly specified fields.
        /// </summary>
        Partial
    }
}
