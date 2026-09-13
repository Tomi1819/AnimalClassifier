namespace AnimalClassifier.Core.DTO
{
    public class AdminAuditLogItem
    {
        public string Action { get; set; } = string.Empty;
        public DateTime DatePerformed { get; set; }
        public string AdminEmail { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }
}
