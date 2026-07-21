using System;
using System.Collections.Generic;

namespace SystemBase.BE.DTOs.SystemRole
{
    public class UpdateRoleFunctionsDto
    {
        public List<Guid> FunctionIds { get; set; } = new List<Guid>();
    }
}
