using Newtonsoft.Json;
using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Web.Services
{
    public class PlexService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string PlexTvUrl = "https://plex.tv";

        public async Task<string> AuthenticateUser(string email, string password)
        {
            try
            {
                var authData = new
                {
                    user = new { login = email, password = password }
                };

                var json = JsonConvert.SerializeObject(authData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                content.Headers.Add("X-Plex-Product", "Kometa GUI v3 Web");
                content.Headers.Add("X-Plex-Version", "1.0");
                content.Headers.Add("X-Plex-Client-Identifier", Guid.NewGuid().ToString());

                var response = await httpClient.PostAsync($"{PlexTvUrl}/users/sign_in.json", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseJson);
                    return result?.user?.authToken;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Plex authentication error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PlexLibrary>> GetLibraries(string serverUrl, string authToken)
        {
            try
            {
                var libraries = new List<PlexLibrary>();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{serverUrl}/library/sections");
                request.Headers.Add("X-Plex-Token", authToken);
                request.Headers.Add("Accept", "application/json");

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseJson);
                    
                    if (result?.MediaContainer?.Directory != null)
                    {
                        foreach (var directory in result.MediaContainer.Directory)
                        {
                            libraries.Add(new PlexLibrary
                            {
                                Name = directory.title,
                                Type = directory.type,
                                IsSelected = false
                            });
                        }
                    }
                }

                return libraries;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching Plex libraries: {ex.Message}");
                return new List<PlexLibrary>();
            }
        }

        public async Task<List<PlexServer>> GetServerList(string authToken)
        {
            try
            {
                var servers = new List<PlexServer>();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{PlexTvUrl}/pms/servers.json");
                request.Headers.Add("X-Plex-Token", authToken);

                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseJson);
                    
                    if (result != null)
                    {
                        foreach (var server in result)
                        {
                            servers.Add(new PlexServer
                            {
                                Name = server.name,
                                Host = server.host,
                                Port = server.port,
                                Address = server.address,
                                MachineIdentifier = server.machineIdentifier
                            });
                        }
                    }
                }

                return servers;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching Plex servers: {ex.Message}");
                return new List<PlexServer>();
            }
        }

        public PlexServer FindBestServer(List<PlexServer> servers)
        {
            // Return first server that appears to be local network
            return servers?.FirstOrDefault(s => 
                s.Address?.StartsWith("192.168.") == true || 
                s.Address?.StartsWith("10.") == true || 
                s.Address?.StartsWith("172.") == true) ?? servers?.FirstOrDefault();
        }
    }
}