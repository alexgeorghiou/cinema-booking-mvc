using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Cinema.Core.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager, string authenticationType)
        {
            var identity = await manager.CreateIdentityAsync(this, authenticationType);
            if (!string.IsNullOrEmpty(DisplayName))
            {
                identity.AddClaim(new Claim("cinema:display_name", DisplayName));
            }

            return identity;
        }
    }

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string BoxOffice = "BoxOffice";
    }
}
