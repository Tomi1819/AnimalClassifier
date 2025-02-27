namespace AnimalClassifier.Infrastructure.Data.Models
{
    public class AnimalRecognitionLog
    {
        public int Id { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public DateTime DateRecognized { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
    }
}
