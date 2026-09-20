namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;

    public interface IPasswordResetService
    {
        /// <summary>
        /// Emails a reset link to the address, if an account has it, and does
        /// nothing at all if none does. Which of the two happened is never
        /// reported, not even as a failure to send, so that the endpoint
        /// cannot be used to find out who is registered.
        /// </summary>
        Task ForgotPasswordAsync(ForgotPasswordRequest request);

        /// <summary>
        /// Sets the new password, if the token belongs to that account and has
        /// not expired. Succeeding changes the account's security stamp, which
        /// ends every session opened with the old password and leaves the link
        /// itself spent.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// When the link cannot be used or the password fails the rules.
        /// </exception>
        Task ResetPasswordAsync(ResetPasswordRequest request);
    }
}
