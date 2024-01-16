using Volo.Abp;

namespace Unity.GrantManager.Web.Exceptions
{
    public class NoGrantProgramsLinkedException : AbpException
    {
        public NoGrantProgramsLinkedException(string? message) : base(message)
        {
        }
    }
}
