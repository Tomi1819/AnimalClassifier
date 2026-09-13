namespace AnimalClassifier.Infrastructure.Data.Models
{
    public class AdminAuditLog
    {
        public int Id { get; set; }
        public AdminAction Action { get; set; }
        public DateTime DatePerformed { get; set; }
        public string AdminId { get; set; } = string.Empty;
        public ApplicationUser Admin { get; set; } = null!;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
    }
}
