using Volo.Abp;

namespace Unity.GrantManager.Integration.Sso
{
    public class FetchTokenException : AbpException
    {
        public FetchTokenException()
        {
        }

        public FetchTokenException(string? message) : base(message)
        {           
        }
    }
}
