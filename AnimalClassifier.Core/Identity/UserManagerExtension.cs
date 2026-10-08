namespace AnimalClassifier.Core.Identity
{
    using AnimalClassifier.Core.Common.Exceptions;
    using AnimalClassifier.Core.Data.Entities;
    using Microsoft.AspNetCore.Identity;
    using static AnimalClassifier.Core.Identity.IdentityMessages;

    public static class UserManagerExtension
    {
        /// <summary>
        /// The user with the id, for a request that cannot go on without one.
        /// </summary>
        /// <exception cref="NotFoundException">
        /// When no user has the id.
        /// </exception>
        public static async Task<ApplicationUser> GetByIdAsync(this UserManager<ApplicationUser> userManager, string userId) =>
            await userManager.FindByIdAsync(userId) ?? throw new NotFoundException(UserNotFound);
    }
}
