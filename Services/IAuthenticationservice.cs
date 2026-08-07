namespace ShuttlOps.Services
{
    public interface IAuthenticationservice
    {
        Task<(bool isSuccess, string message)> AuthenticateUser(string username, string password);
    }
}
