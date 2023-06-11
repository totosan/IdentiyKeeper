using Orleans.Runtime;

namespace Grains;

public class UserRegistryGrain : IGrain, IUserRegistryGrain
{
    private readonly IPersistentState<UserRegistry> _state;

    public UserRegistryGrain([PersistentState("ureg","uregStore")]IPersistentState<UserRegistry> state)
    {
        _state = state;
    }

    
    public Task AddUser(string username)
    {
        _state.State.Users.Add(username);
        return _state.WriteStateAsync();
    }

    public Task<List<string>> GetUsers()
    {
        return Task.FromResult(_state.State.Users);
    }

    public Task RemoveUser(string username)
    {
        _state.State.Users.Remove(username);
        return _state.WriteStateAsync();
    }
}

[GenerateSerializer]
public class UserRegistry
{
    public List<string> Users { get; set; } = new List<string>();
}
