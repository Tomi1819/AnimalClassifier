namespace AnimalClassifier.Core.DTO
{
    public class AdminUserItem
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateRegistered { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsLocked { get; set; }
        public int RecognitionCount { get; set; }
    }
}
