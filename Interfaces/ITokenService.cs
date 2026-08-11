using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.Models;

namespace MIC.risk.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(AppUser user, IEnumerable<string> roles);
    }
}