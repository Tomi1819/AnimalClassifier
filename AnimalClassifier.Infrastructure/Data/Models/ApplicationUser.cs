namespace AnimalClassifier.Infrastructure.Data.Models
{
    using Microsoft.AspNetCore.Identity;
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime DateRegistered { get; set; }
    }
}
