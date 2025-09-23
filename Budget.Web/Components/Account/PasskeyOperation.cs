namespace Budget.Web.Components.Account
{
    /// <summary>
    /// Represents the type of passkey operation being performed.
    /// </summary>
    public enum PasskeyOperation
    {
        /// <summary>
        /// Creating a new passkey credential.
        /// </summary>
        Create,

        /// <summary>
        /// Requesting authentication with an existing passkey credential.
        /// </summary>
        Request
    }
}