using Dapper;
using Microsoft.IdentityModel.Tokens;
using RefreshTokenApi.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RefreshTokenApi.Dapper.DapperService
{
    public interface IAccountService
    {
        Task<bool> Register(Register model);
        Task<Authentication> Login (Login model);
        Task<Token> RefreshToken(Token model);
    }

    public class AccountService : IAccountService
    {
        private readonly IDapperConfiguration _dapperConfig;
        private readonly IConfiguration _config;

        public AccountService(IConfiguration config)
        {
            _dapperConfig = new DapperConfiguration();
            _config = config;   
        }

        public async Task<Authentication> Login(Login model)
        {
            using var connection = _dapperConfig.Connection;
            using var transaction = connection.BeginTransaction();
            Authentication response = new();
            try
            {
                string query1 = @"Select * from Users";

                var dbUsers = connection.Query(query1, transaction : transaction).ToList();
                
                if(dbUsers.Any())
                {
                    var user = dbUsers.Where(x => x.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase)
                                                || x.Password.Equals(model.Password, StringComparison.InvariantCultureIgnoreCase))
                                                    .FirstOrDefault();

                    if(user != null)
                    {
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
                        var claims = new[]
                        {
                            new Claim("Id", user.Id.ToString()),
                            new Claim(ClaimTypes.Name, user.Name),
                        };

                        var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                            _config["Jwt:Issuer"],
                            claims,
                            expires: DateTime.Now.AddMinutes(1),
                            signingCredentials: credentials);

                        var generateToken = new JwtSecurityTokenHandler().WriteToken(token);
                        var refreshToken = GenerateRefreshToken();
                        var tokenExpiryDate = DateTime.Now.AddMinutes(1);

                        response.JwtToken = generateToken;
                        response.RefreshToken = refreshToken;

                        string query2 = @"Update Users set AccessToken = @AccessToken, 
                                                           RefreshToken = @RefreshToken,
                                                           TokenExpiryDate = @expireDate
                                          where Id = @id";

                        var result = await connection.ExecuteAsync(query2, new
                        {
                            id = user.Id,
                            AccessToken = generateToken,
                            RefreshToken = refreshToken,
                            expireDate = tokenExpiryDate
                        }, transaction: transaction);

                        transaction.Commit();
                        connection.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                transaction.Rollback();
                connection.Close();
                throw;
            }
            return response;
        }

        public async Task<bool> Register(Register model)
        {
            using var connection = _dapperConfig.Connection;
            using var transaction = connection.BeginTransaction();  

            try
            {
                var id = Guid.NewGuid();
                string query = @"Insert into Users(Id, Name, Password)
                                Values (@Id, @Name, @Password)";

                var result = await connection.ExecuteAsync(query, new
                {
                    Id = id,
                    Name = model.UserName,
                    Password = model.Password,
                }, transaction);

                transaction.Commit();
                connection.Close();
                return true;
            }
            catch(Exception ex)
            {
                transaction.Rollback();
                connection.Close();
                throw;
            }
        }

        public async Task<Token> RefreshToken(Token model)
        {
            using var connection = _dapperConfig.Connection;
            using var transaction = connection.BeginTransaction();
            Token response = new();

            try
            {
                string accessToken = model.AccessToken;
                string refreshToken = model.RefreshToken;

                    var principal = GetPrincipalFromExpiredToken(accessToken);

                    if (principal == null)
                    {
                    return response;
                    }

                var userId = Guid.Parse(principal.Claims.FirstOrDefault().Value);

                string query = @"Select * from Users where Id = @Id";

                var dbUser = await connection.QueryFirstOrDefaultAsync<User>(query, new
                {
                    Id = userId
                }, transaction);

                var newAccessToken = new JwtSecurityTokenHandler().
                                     WriteToken(CreateToken(principal.Claims.ToList()));
                var newRefreshToken = GenerateRefreshToken();
                var tokenExpiryDate = DateTime.Now.AddMinutes(1);

                response.AccessToken = newAccessToken;
                response.RefreshToken = newRefreshToken;

                string query2 = @"Update Users set AccessToken = @AccessToken, 
                                                           RefreshToken = @RefreshToken,
                                                           TokenExpiryDate = @expireDate
                                          where Id = @id";

                var result = await connection.ExecuteAsync(query2, new
                {
                    id = userId,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    expireDate = tokenExpiryDate
                }, transaction: transaction);

                transaction.Commit();
                connection.Close();

            }
            catch(Exception ex)
            {
                throw ex;
            }

            return response;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;

        }

        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]));
            _ = int.TryParse(_config["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);

            var token = new JwtSecurityToken(
                issuer: _config["JWT:Issuer"],
                audience: _config["JWT:Issuer"],
                expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;
        }
    }
}
