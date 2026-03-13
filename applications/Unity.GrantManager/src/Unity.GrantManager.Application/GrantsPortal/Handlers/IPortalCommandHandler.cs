using System.Threading.Tasks;
using Unity.GrantManager.GrantsPortal.Messages;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public interface IPortalCommandHandler
{
    string DataType { get; }
    Task<string> HandleAsync(PluginDataPayload payload);
}
