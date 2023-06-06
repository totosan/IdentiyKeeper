using Orleans.Runtime;


public class UserIdentityGrain : Grain, IUserIdentityGrain
{
    private readonly IPersistentState<UserIdentity> _state;
    public UserIdentityGrain([PersistentState("user", "users")] IPersistentState<UserIdentity> state)
    {
        _state = state;
        _state.State.Name = this.GetPrimaryKeyString();
        _state.State.Email = "";
        _state.State.ActionName = "Created";
    }

    Task<string> IUserIdentityGrain.GetActionName()
    {
        return Task.FromResult(_state.State.ActionName??"");
    }

    Task<string> IUserIdentityGrain.GetEmail()
    {
        return Task.FromResult(_state.State.Email ?? "");
    }

    Task<string> IUserIdentityGrain.GetName()
    {
        return Task.FromResult(_state.State.Name ?? "");
    }

    Task IUserIdentityGrain.SetActionName(string actionName)
    {
        _state.State.ActionName = actionName;
        return _state.WriteStateAsync();
    }

    Task IUserIdentityGrain.SetEmail(string email)
    {
        _state.State.Email = email;
        return _state.WriteStateAsync();
    }

    Task IUserIdentityGrain.SetName(string name)
    {
        _state.State.Name = name;
        return _state.WriteStateAsync();
    }
}

[GenerateSerializer]
public class UserIdentity
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? ActionName { get; set; }
}