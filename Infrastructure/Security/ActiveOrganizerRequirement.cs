using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Security
{
    public class ActiveOrganizerRequirement : IAuthorizationRequirement
    {
        // marker requirement - no data needed
    }
}

