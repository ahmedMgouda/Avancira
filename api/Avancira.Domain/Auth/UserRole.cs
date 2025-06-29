using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avancira.Domain.Catalog.Enums
{
    [Flags]
    public enum UserRole
    {
        None = 0,
        Student = 1, // 01 in binary
        Tutor = 2    // 10 in binary
    }
}
