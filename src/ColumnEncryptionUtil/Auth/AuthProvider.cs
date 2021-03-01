using System;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;

namespace ColumnEncryption.Util.Auth
{
    public class AuthProvider : IAuthProvider
    {
        private readonly string applicationId;
        private readonly string applicationUri;
        private readonly TokenCache tokenCache;

        public AuthProvider(string applicationId, string applicationUri)
        {
            // Disable for use for Managed Identity 
            // if (string.IsNullOrWhiteSpace(applicationId)) throw new ArgumentNullException(applicationId);
            // if (string.IsNullOrWhiteSpace(applicationUri)) throw new ArgumentNullException(applicationUri);

            this.applicationId = applicationId;
            this.applicationUri = applicationUri;
            this.tokenCache = new TokenCache();
        }

        /*
        public string AcquireToken(string authority, string resource, string claims)
        {
            AuthenticationContext authContext = new AuthenticationContext(authority, this.tokenCache);

            string accessToken = Task.Run(() => AcquireTokenSilentAsync(authContext, resource, this.applicationId)).GetAwaiter().GetResult();
            if (accessToken != null) return accessToken;

            AuthenticationResult result = Task.Run(() => authContext.AcquireTokenAsync(
                    resource,
                    this.applicationId,
                    new Uri(this.applicationUri),
                    new PlatformParameters(PromptBehavior.SelectAccount),
                    UserIdentifier.AnyUser)).GetAwaiter().GetResult();

            return result.AccessToken;
        }
        */

        public async Task<string> AcquireTokenAsync(string authority, string resource, string scope)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            return accessToken;

            /*
            AuthenticationContext authContext = new AuthenticationContext(authority, this.tokenCache);

            string accessToken = await AcquireTokenSilentAsync(authContext, resource, this.applicationId).ConfigureAwait(false);
            if (accessToken != null) return accessToken;

            AuthenticationResult result = await authContext.AcquireTokenAsync(
                    resource,
                    this.applicationId,
                    new Uri(this.applicationUri),
                    new PlatformParameters(PromptBehavior.SelectAccount),
                    UserIdentifier.AnyUser);

            return result.AccessToken;
            */
        }

        private async Task<string> AcquireTokenSilentAsync(AuthenticationContext authenticationContext, string resource, string clientId)
        {
            try
            {
                AuthenticationResult result = await authenticationContext.AcquireTokenSilentAsync(resource, clientId).ConfigureAwait(false);
                return result.AccessToken;
            }
            catch (Exception ex) when (ex.GetBaseException() is AdalSilentTokenAcquisitionException)
            {
                return null;
            }
        }

        // This method fetches a token from Azure Active Directory, which can then be provided to Azure Key Vault to authenticate
        public async Task<string> GetAccessTokenAsync()
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");
            return accessToken;
        }
    }
}