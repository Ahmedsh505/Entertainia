using System.Security.Claims;
using Core.Enums;
using Infrastructure.Data.DbContexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security
{
    public class ActiveOrganizerHandler : AuthorizationHandler<ActiveOrganizerRequirement>
    {
        private readonly EntertaniaDbcontext _db;

        public ActiveOrganizerHandler(EntertaniaDbcontext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveOrganizerRequirement requirement)
        {
            // If user is not authenticated, fail
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return;
            }

            // Admins are always allowed
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Get the user id from claims
            var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim))
            {
                context.Fail();
                return;
            }

            if (!int.TryParse(idClaim, out var userId))
            {
                context.Fail();
                return;
            }

            // Load user from DB and check account status
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                context.Fail();
                return;
            }

            // If the user is an Organizer and Active -> succeed
            if (user.Role == "Organizer" && user.AccountStatus == AccountStatus.Active)
            {
                context.Succeed(requirement);
                return;
            }

            // Otherwise fail
            context.Fail();
        }
    }
}
