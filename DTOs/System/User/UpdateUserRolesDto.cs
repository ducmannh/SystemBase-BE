using System;
using System.Collections.Generic;

namespace SystemBase.BE.DTOs.User
{
    public class UpdateUserRolesDto
    {
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }
}
