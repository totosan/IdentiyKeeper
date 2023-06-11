public interface IUserRegistryGrain: IGrainWithIntegerKey
{
    //add username to list
    Task AddUser(string username);
    //remove from list
    Task RemoveUser(string username);

    //get list of users
    Task<List<string>> GetUsers();
}
