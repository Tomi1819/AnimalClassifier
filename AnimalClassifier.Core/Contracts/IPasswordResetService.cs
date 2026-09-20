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
    }
}
