using System.Threading.Tasks;

namespace Unity.GrantManager.Emails
{
    public interface IEmailAppService
    {
        Task<bool> CreateAsync(CreateEmailDto dto);
    }
}
