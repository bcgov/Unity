namespace Unity.TenantManagement.Application.Contracts
{
    public class TenantDbCredentials
    {
        public TenantDbCredentials(string dbName, string username, string password)
        {
            DbName = dbName;
            Username = username;
            Password = password;
        }

        public string DbName { get; }
        public string Username { get; }
        public string Password { get; }
    }
}
