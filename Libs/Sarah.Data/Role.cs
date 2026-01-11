using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sarah.Data
{
    public class Role : Microsoft.AspNetCore.Identity.IdentityRole
    {
        public const string AdminRoleName = "admins";
        public const string UsersRoleName = "users";
    }
}
