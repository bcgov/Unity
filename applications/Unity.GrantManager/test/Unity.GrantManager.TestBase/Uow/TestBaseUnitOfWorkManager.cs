using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Uow
{
    [Dependency(ReplaceServices = true)]
    public class TestBaseUnitOfWorkManager : IUnitOfWorkManager, ISingletonDependency
    {
        private readonly UnitOfWorkManager _innerUnitOfWorkManager;

        public TestBaseUnitOfWorkManager(UnitOfWorkManager innerUnitOfWorkManager)
        {
            _innerUnitOfWorkManager = innerUnitOfWorkManager;
        }

        public IUnitOfWork Begin(AbpUnitOfWorkOptions options, bool requiresNew = false)
        {
            options.IsTransactional = false;
            return _innerUnitOfWorkManager.Begin(options, requiresNew);
        }

        public IUnitOfWork Reserve(string reservationName, bool requiresNew = false)
        {
            return _innerUnitOfWorkManager.Reserve(reservationName, requiresNew);
        }

        public void BeginReserved(string reservationName, AbpUnitOfWorkOptions options)
        {
            options.IsTransactional = false;
            _innerUnitOfWorkManager.BeginReserved(reservationName, options);
        }

        public bool TryBeginReserved(string reservationName, AbpUnitOfWorkOptions options)
        {
            options.IsTransactional = false;
            return _innerUnitOfWorkManager.TryBeginReserved(reservationName, options);
        }

        public IUnitOfWork? Current => _innerUnitOfWorkManager.Current;
    }
}
