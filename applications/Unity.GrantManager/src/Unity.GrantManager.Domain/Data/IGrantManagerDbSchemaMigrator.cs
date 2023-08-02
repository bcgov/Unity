using System.Threading.Tasks;

namespace Unity.GrantManager.Data;

public interface IGrantManagerDbSchemaMigrator
{
    Task MigrateAsync();
}
