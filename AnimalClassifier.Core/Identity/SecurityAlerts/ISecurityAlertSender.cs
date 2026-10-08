namespace AnimalClassifier.Core.Identity.SecurityAlerts
{
    using AnimalClassifier.Core.Data.Entities;

    /// <summary>
    /// Emails a user when how their account is signed in to changes, so that
    /// someone else making the change does not go unnoticed.
    ///
    /// Each alert is sent once the change has been made, and never fails: a
    /// mail server that is down is logged rather than reported, since an error
    /// would tell the user that a change which went through had not.
    /// </summary>
    public interface ISecurityAlertSender
    {
        Task PasswordChangedAsync(ApplicationUser user);

        Task PasskeyAddedAsync(ApplicationUser user, string passkeyName);

        Task PasskeyRemovedAsync(ApplicationUser user, string passkeyName);

        Task OtherSessionsSignedOutAsync(ApplicationUser user);
    }
}
