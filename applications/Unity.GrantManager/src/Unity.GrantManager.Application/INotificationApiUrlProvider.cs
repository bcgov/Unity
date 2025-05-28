
using System.Threading.Tasks;

namespace Unity.GrantManager;
public interface INotificationApiUrlProvider
{
    Task<string> GetBaseUrlAsync();
}