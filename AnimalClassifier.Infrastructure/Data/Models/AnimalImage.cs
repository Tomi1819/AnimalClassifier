namespace AnimalClassifier.Infrastructure.Data.Models
{
    public class AnimalImage
    {
        public int Id { get; set; }
        public string AnimalName { get; set; } = string.Empty;
        public string ImageData { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
    }
}
