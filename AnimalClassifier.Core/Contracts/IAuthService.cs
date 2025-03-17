namespace AnimalClassifier.Core.Contracts
{
    using AnimalClassifier.Core.DTO;
    public interface IAuthService
    {
        Task<RegisterResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LogInRequest request);
    }
}
