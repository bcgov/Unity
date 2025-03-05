using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;

namespace Unity.GrantManager.Repositories
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IUserAccountsRepository))]
    public class UserAccountsRepository : EfCoreRepository<GrantManagerDbContext, IdentityUser, Guid>, IUserAccountsRepository
    {
        public UserAccountsRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : base(dbContextProvider)
        {
        }

        public async Task<IList<IdentityUser>> GetListByOidcSub(string oidcSub)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet.AsQueryable()
                .Where(u => EF.Functions.ILike(EF.Property<string>(u, "OidcSub"), $"{oidcSub.ToSubjectWithoutIdp()}%"))
                .ToListAsync();
        }

        public async Task<int> UpdateDefinition(string name, string value)
        {
            var dbContext = await GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE \"SettingDefinitions\" SET \"DefaultValue\" = @defaultValue WHERE \"Name\" = @name";
                var defaultValueParam = command.CreateParameter();
                defaultValueParam.ParameterName = "@defaultValue";
                defaultValueParam.DbType = DbType.String;
                defaultValueParam.Value = value;
                command.Parameters.Add(defaultValueParam);

                var nameParam = command.CreateParameter();
                nameParam.ParameterName = "@name";
                nameParam.DbType = DbType.String;
                nameParam.Value = name;
                command.Parameters.Add(nameParam);

                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                return await command.ExecuteNonQueryAsync();
            }
        }

    }
}
