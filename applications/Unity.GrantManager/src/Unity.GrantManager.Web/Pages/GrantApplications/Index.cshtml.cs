using Keycloak.Net;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Keycloak.Net.Models.Users;
using Keycloak.Net.Models.Groups;

namespace Unity.GrantManager.Web.Pages.GrantApplications
{
    public class IndexModel : GrantManagerPageModel
    {

        private readonly KeycloakClient _keycloakClient;

        public IEnumerable<User> groupUsers { get; set; }

        public IndexModel(KeycloakClient keycloakClient)
        {
            _keycloakClient = keycloakClient;
        }

        [BindProperty(SupportsGet = true)]
        public Guid? FormId { get; set; }
        public async Task OnGetAsync()
        {
            // We should know the group based on the logged on user selection of grant programs
            string realm = "master";
            string search = "MJF";

            /*IEnumerable<Group> groups = await _keycloakClient.GetGroupHierarchyAsync(realm, search: search).ConfigureAwait(false);
            string groupId = groups.FirstOrDefault()?.Id;
            if (groupId != null)
            {
               groupUsers = await _keycloakClient.GetGroupUsersAsync(realm, groupId).ConfigureAwait(false);
            }*/

            IEnumerable<User> users = await _keycloakClient.GetUsersAsync(realm).ConfigureAwait(false);
        }
    }
}
