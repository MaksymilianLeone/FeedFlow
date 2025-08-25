using Microsoft.AspNetCore.Identity;

namespace FeedFlow.Web.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public Guid OrgId { get; set; }
    }
}
