namespace SmartCityParking.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> ValidateCredentialsAsync(string username, string password);
    }
}