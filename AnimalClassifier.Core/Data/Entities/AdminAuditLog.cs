namespace AnimalClassifier.Core.Data.Entities
{
    public class AdminAuditLog
    {
        public int Id { get; set; }
        public AdminAction Action { get; set; }
        public DateTime DatePerformed { get; set; }

        /// <summary>
        /// Null once the administrator has deleted their account. The entry
        /// stays, so the log still shows everything that was done.
        /// </summary>
        public string? AdminId { get; set; }
        public ApplicationUser? Admin { get; set; }

        /// <summary>
        /// Null once the user has deleted their account, for the same reason.
        /// </summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
