namespace AnimalClassifier.Core.DTO
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
