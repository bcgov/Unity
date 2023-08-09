using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;

namespace Unity.GrantManager.Controllers
{
    [Authorize]
    [Route("/api/identity-profile")]
    public class IdentityProfileController : GrantManagerController, IIdentityProfileAppService
    {
        protected IIdentityProfileAppService IdentityProfileAppService { get; }

        public IdentityProfileController(IIdentityProfileAppService identityProfileAppService)
        {
            IdentityProfileAppService = identityProfileAppService;
        }

        [Route("create-or-update")]
        [HttpPost]
        public Task CreateOrUpdateAsync()
        {
            return IdentityProfileAppService.CreateOrUpdateAsync();
        }
    }
}
