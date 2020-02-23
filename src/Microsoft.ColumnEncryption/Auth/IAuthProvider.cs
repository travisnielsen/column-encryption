using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ColumnEncryption.Auth
{
    public interface IAuthProvider
    {
        Task<string> AcquireTokenAsync(string authority, string resource, string scope);

        // string AcquireToken(string authority, string resource, string claims);

        Task<string> GetAccessTokenAsync();
    }
}