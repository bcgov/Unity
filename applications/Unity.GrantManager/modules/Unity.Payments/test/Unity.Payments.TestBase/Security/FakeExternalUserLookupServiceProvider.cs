using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.Payments.Security;

public class FakeExternalUserLookupServiceProvider : IExternalUserLookupServiceProvider, ITransientDependency
{
    public Task<IUserData> FindByIdAsync(Guid id, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.FromResult<IUserData>(id switch
        {
            _ when id == PaymentsTestData.UserDataMocks.User1.Id => PaymentsTestData.UserDataMocks.User1,
            _ when id == PaymentsTestData.UserDataMocks.User2.Id => PaymentsTestData.UserDataMocks.User2,
            _ when id == PaymentsTestData.UserDataMocks.User3.Id => PaymentsTestData.UserDataMocks.User3,
            _ when id == PaymentsTestData.UserDataMocks.User4.Id => PaymentsTestData.UserDataMocks.User4,
            _ when id == PaymentsTestData.UserDataMocks.User5.Id => PaymentsTestData.UserDataMocks.User5,
            _ => new UserData(id, id.ToString())
        });
    }

    public Task<IUserData> FindByUserNameAsync(string userName,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotSupportedException();
    }

    public Task<List<IUserData>> SearchAsync(string? sorting = null, string? filter = null,
        int maxResultCount = 2147483647, int skipCount = 0,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotSupportedException();
    }

    public Task<long> GetCountAsync(string? filter = null, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotSupportedException();
    }
}
