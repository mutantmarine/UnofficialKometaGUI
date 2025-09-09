using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace KometaGUIv3.Services
{
    public class PlexOAuthService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string PlexTvUrl = "https://plex.tv";
        private const string PlexAuthUrl = "https://app.plex.tv/auth";
        private const int PollingIntervalMs = 1000; // Poll every 1 second
        private const int TimeoutMinutes = 5; // 5-minute timeout for authentication

        private readonly string clientIdentifier;
        private CancellationTokenSource cancellationTokenSource;

        public PlexOAuthService()
        {
            clientIdentifier = Guid.NewGuid().ToString();
        }

        public class PlexPin
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            
            [JsonProperty("code")]
            public string Code { get; set; }
            
            [JsonProperty("authToken")]
            public string AuthToken { get; set; }
            
            [JsonProperty("expiresAt")]
            public DateTime ExpiresAt { get; set; }
        }

        public class PlexPinResponse
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            
            [JsonProperty("code")]
            public string Code { get; set; }
            
            [JsonProperty("authToken")]
            public string AuthToken { get; set; }
        }

        public async Task<string> AuthenticateWithBrowser()
        {
            try
            {
                // Cancel any existing authentication attempt
                CancelCurrentAuthentication();
                cancellationTokenSource = new CancellationTokenSource();

                // Step 1: Generate PIN
                var pin = await GeneratePin();
                if (pin == null)
                {
                    throw new Exception("Failed to generate authentication PIN");
                }

                // Step 2: Construct OAuth URL and open browser
                var authUrl = ConstructAuthUrl(pin);
                OpenBrowser(authUrl);

                // Step 3: Poll for authentication completion
                var token = await PollForToken(pin.Id, pin.Code, cancellationTokenSource.Token);
                
                return token;
            }
            catch (OperationCanceledException)
            {
                throw new Exception("Authentication was cancelled");
            }
            catch (Exception ex)
            {
                throw new Exception($"OAuth authentication failed: {ex.Message}");
            }
        }

        private async Task<PlexPin> GeneratePin()
        {
            try
            {
                // Use form-encoded data as per Plex API documentation
                var formParams = new List<KeyValuePair<string, string>>
                {
                    new("strong", "true")
                };
                var content = new FormUrlEncodedContent(formParams);

                // Create request with proper headers
                var request = new HttpRequestMessage(HttpMethod.Post, $"{PlexTvUrl}/api/v2/pins")
                {
                    Content = content
                };
                
                // Add required headers to request (not content)
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-Plex-Product", "Unofficial Kometa GUI");
                request.Headers.Add("X-Plex-Version", "1.0");
                request.Headers.Add("X-Plex-Client-Identifier", clientIdentifier);

                var response = await httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var pinData = JsonConvert.DeserializeObject<PlexPin>(responseJson);
                    return pinData;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PIN generation failed: {ex.Message}");
                return null;
            }
        }

        private string ConstructAuthUrl(PlexPin pin)
        {
            var parameters = new Dictionary<string, string>
            {
                ["clientID"] = clientIdentifier,
                ["context[device][product]"] = "Unofficial Kometa GUI",
                ["context[device][version]"] = "1.0",
                ["context[device][platform]"] = Environment.OSVersion.Platform.ToString(),
                ["context[device][platformVersion]"] = Environment.OSVersion.Version.ToString(),
                ["context[device][device]"] = $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}",
                ["context[device][deviceName]"] = Environment.MachineName,
                ["context[device][model]"] = "Kometa GUI OAuth",
                ["context[device][layout]"] = "desktop",
                ["code"] = pin.Code
            };

            var queryString = string.Join("&", parameters.Select(kvp => 
                $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            return $"https://app.plex.tv/auth/#!?{queryString}";
        }

        private void OpenBrowser(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open browser: {ex.Message}");
            }
        }

        private async Task<string> PollForToken(int pinId, string pinCode, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromMinutes(TimeoutMinutes);

            while (DateTime.UtcNow - startTime < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var pin = await CheckPinStatus(pinId, pinCode);
                    
                    if (pin != null && !string.IsNullOrEmpty(pin.AuthToken))
                    {
                        // Authentication successful!
                        return pin.AuthToken;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"PIN status check failed: {ex.Message}");
                    // Continue polling even if individual requests fail
                }

                // Wait before next poll
                await Task.Delay(PollingIntervalMs, cancellationToken);
            }

            throw new TimeoutException("Authentication timed out. Please try again.");
        }

        private async Task<PlexPinResponse> CheckPinStatus(int pinId, string pinCode)
        {
            try
            {
                // Use form-encoded data for PIN status check as per documentation
                var formParams = new List<KeyValuePair<string, string>>
                {
                    new("code", pinCode)
                };
                var content = new FormUrlEncodedContent(formParams);

                var request = new HttpRequestMessage(HttpMethod.Get, $"{PlexTvUrl}/api/v2/pins/{pinId}")
                {
                    Content = content
                };
                
                // Add required headers
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-Plex-Client-Identifier", clientIdentifier);

                var response = await httpClient.SendAsync(request);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var pinResponse = JsonConvert.DeserializeObject<PlexPinResponse>(responseJson);
                    return pinResponse;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PIN status check failed: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{PlexTvUrl}/api/v2/user");
                request.Headers.Add("X-Plex-Token", token);
                
                var response = await httpClient.SendAsync(request);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public void CancelCurrentAuthentication()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }

        public void Dispose()
        {
            CancelCurrentAuthentication();
        }
    }
}