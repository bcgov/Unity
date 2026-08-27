using System.Threading.Tasks;

namespace Unity.Modules.Shared.Specializations;

public interface ISpecializationChecker
{
    Task<bool> IsEnabledAsync(string name);
}
