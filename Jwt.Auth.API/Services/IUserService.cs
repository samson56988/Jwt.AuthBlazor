using Jwt.Auth.API.Dto;

namespace Jwt.Auth.API.Services
{
    public interface IUserService
    {
        Task<(bool IsUserRegistered, string Message)> RegisterNewUserAsync(UserRegistrationDto userRegistrationDto);

        bool CheckUserUniqueEmail(string email);    

        Task<(bool IsLoginSuccess, JwtTokenResponseDto Token)> LoginAsync(LoginDto loginPayLoad);
    }
}
