using System.Net;
using Dapper;
using RefreshTokenApi.Dapper;
using RefreshTokenApi.Dapper.DapperService;
using RefreshTokenApi.DTO;

namespace RefreshTokenApi.Middleware
{
    public class ApplicationState
    {
        private readonly RequestDelegate _next;
        private readonly IDapperConfiguration _dapperConfig;

        public ApplicationState(RequestDelegate next)
        {
            _next = next;   
            _dapperConfig = new DapperConfiguration();
        }

        public async Task Invoke(HttpContext context , IAccountService _accountService)
        {
            try
            {
                if(context.Request.Path != "/api/Account/Login")
                {
                    var checkTokenExpiryTime = await CheckRefreshToken(context , _accountService);
                }
         
                await _next(context);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<bool> CheckRefreshToken(HttpContext context , IAccountService _accountService)
        {
            if (context.User?.Claims?.FirstOrDefault() != null)
            {
                var claimValue = context.User.Claims.FirstOrDefault()?.Value;

                if (Guid.TryParse(claimValue, out Guid claimUserId))
                {
                    using (var connection = _dapperConfig.Connection)
                    {
                        string query = @"Select * from Users where Id = @Id";

                        var dbUser = await connection.QueryFirstOrDefaultAsync<User>(query, new
                        {
                            @Id = claimUserId,
                        });

                        if (dbUser != null)
                        {
                            if(DateTime.Now >= dbUser.TokenExpiryDate)
                            {
                                Token reqToken = new Token
                                {
                                    AccessToken = dbUser.AccessToken,
                                    RefreshToken = dbUser.RefreshToken
                                };

                                await _accountService.RefreshToken(reqToken);
                            }
                           
                        }

                    }
                    return true; 
                }
            }

            return false;
        }
    }
}
