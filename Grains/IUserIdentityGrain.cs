public interface IUserIdentityGrain : IGrainWithStringKey
{
    Task SetName(string name);
    Task<string> GetName();

    Task<string> GetEmail();
    Task SetEmail(string email);
    
    Task<string> GetActionName();
    Task SetActionName(string actionName);

    Task ClearState();
}