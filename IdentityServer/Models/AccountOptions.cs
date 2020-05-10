using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Models
{
    public class AccountOptions
    {
        public string SPAUrl { get; set; }
        public int MaxFailedTwoFactorAccessAttempts { get; set; }
        public TimeSpan TwoFactorCodeValidityPeriod { get; set; }
        public TimeSpan DefaultLockoutTimeSpan { get; set; }
    }
}
