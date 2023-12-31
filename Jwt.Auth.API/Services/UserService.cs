using Jwt.Auth.API.Data;
using Jwt.Auth.API.Data.Entities;
using Jwt.Auth.API.Dto;
using Jwt.Auth.API.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Jwt.Auth.API.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _appDbContext;
        private readonly TokenSettings _tokenSettings;

        public UserService(AppDbContext appDbContext,IOptions<TokenSettings> tokenSettings)
        {
            _appDbContext = appDbContext;
            _tokenSettings = tokenSettings.Value;
        }

        private User FromUserRegistrationModelToUserModel(UserRegistrationDto userRegistrationDto)
        {
            return new User
            {
                Email = userRegistrationDto.Email,
                FirstName = userRegistrationDto.FirstName,
                LastName = userRegistrationDto.LastName,
                Password = userRegistrationDto.Password,
            };
        }

        private string HashPassword(string plainPassword)
        {
            byte[] salt = new byte[16];
            using(var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var rfcPassword = new Rfc2898DeriveBytes(plainPassword, salt, 1000, HashAlgorithmName.SHA1);
            byte[] rfcPasswordHash = rfcPassword.GetBytes(20);

            byte[] passwordHash = new byte[36];

            Array.Copy(salt, 0, passwordHash, 0 , 16);
            Array.Copy(rfcPasswordHash, 0, passwordHash, 16, 20);

            return Convert.ToBase64String(passwordHash);
        }

        public async Task<(bool IsUserRegistered, string Message)> RegisterNewUserAsync(UserRegistrationDto userRegistrationDto)
        {
            var isUserExist = _appDbContext.User.Any(_ => _.Email.ToLower() == userRegistrationDto.Email.ToLower());
            if (isUserExist)
            {
                return (false, "Email Address Already Registred");
            }

            var newUser =  FromUserRegistrationModelToUserModel(userRegistrationDto);
            newUser.Password = HashPassword(newUser.Password);

            _appDbContext.User.Add(newUser);
            await _appDbContext.SaveChangesAsync();
            return (true, "Success");

        }

        public bool CheckUserUniqueEmail(string email)
        {
            var userAlreadyExist = _appDbContext.User.Any(_ => _.Email.ToLower() == email.ToLower());
            return !userAlreadyExist;
        }

        private string GenerateJwtToken(User user)
        {
            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenSettings.SecretKey));

            var credentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>();
            claims.Add(new Claim("Sub",user.Id.ToString()));
            claims.Add(new Claim("FirstName", user.FirstName));
            claims.Add(new Claim("LastName", user.LastName));
            claims.Add(new Claim("Email", user.Email));

            var securityToken = new JwtSecurityToken(
                issuer: _tokenSettings.Issuer,
                audience: _tokenSettings.Audience,
                expires: DateTime.Now.AddMinutes(30),
                claims: claims);


            return new JwtSecurityTokenHandler().WriteToken(securityToken);
                
        }

        public async Task<(bool IsLoginSuccess, JwtTokenResponseDto Token)> LoginAsync(LoginDto loginPayLoad)
        {
            if(string.IsNullOrEmpty(loginPayLoad.Email) || string.IsNullOrEmpty(loginPayLoad.Password))
            {
                return (false,null);
            }

            var user = await _appDbContext.User.Where(_ => _.Email.ToLower() == loginPayLoad.Email.ToLower()).FirstOrDefaultAsync();

            if(user == null)
            {

                return (false, null);
            }

            bool validPassword =  PasswordVerification(loginPayLoad.Password, user.Password);
            if (!validPassword)
            {

                return (false, null);
            }

            var jwtAccessToken = GenerateJwtToken(user);

            var result = new JwtTokenResponseDto
            {
                AccessToken = jwtAccessToken,
            };

            return (true, result);  
        }

        private bool PasswordVerification(string plainPassword , string dbPassword)
        {
            byte[] dbPasswordHash = Convert.FromBase64String(dbPassword);

            byte[] salt = new byte[16];
            Array.Copy(dbPasswordHash, 0, salt,0, 16);

            var rfcPassword = new Rfc2898DeriveBytes(plainPassword, salt, 1000, HashAlgorithmName.SHA1);
            byte[] rfcPasswordHash = rfcPassword.GetBytes(20);

            for(int i = 0; i < rfcPasswordHash.Length; i++)
            {
                if (dbPasswordHash[i + 16] != rfcPasswordHash[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
