namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;

    public interface IPasswordResetService
    {
        /// <summary>
        /// Emails a reset link to the address, if an account has it, and does
        /// nothing at all if none does. Which of the two happened is not
        /// reported, so that the endpoint cannot be used to find out who is
        /// registered. A failure to send is logged rather than thrown, since
        /// an error reaching the caller would answer that same question.
        /// </summary>
        Task ForgotPasswordAsync(ForgotPasswordRequest request);

        /// <summary>
        /// Sets the new password, if the token belongs to that account and has
        /// not expired. Succeeding changes the account's security stamp, which
        /// ends every session opened with the old password and leaves the link
        /// itself spent.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When the link is not one this account can use, or when the new
        /// password fails the rules. The message explains which, except that
        /// an unknown address and an unusable token are deliberately told
        /// apart by nothing.
        /// </exception>
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }
}
