using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using KometaGUIv3.Models;
using Newtonsoft.Json;

namespace KometaGUIv3.Services
{
    public class PlexService
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string PlexTvUrl = "https://plex.tv";
        
        // Cache for libraries to avoid repeated API calls
        private static readonly Dictionary<string, CachedLibraries> libraryCache = new Dictionary<string, CachedLibraries>();
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(10); // Cache for 10 minutes

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

                content.Headers.Add("X-Plex-Product", "Kometa GUI v3");
                content.Headers.Add("X-Plex-Version", "1.0");
                content.Headers.Add("X-Plex-Client-Identifier", Guid.NewGuid().ToString());

                var response = await httpClient.PostAsync($"{PlexTvUrl}/users/sign_in.json", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseJson);
                    return result?.user?.authentication_token?.ToString();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Authentication failed: {ex.Message}");
            }
        }

        public async Task<List<PlexServer>> GetServerList(string token)
        {
            try
            {
                var servers = new List<PlexServer>();
                
                // Correct endpoint - plex.tv/pms/servers.xml (but request JSON via Accept header)
                var url = $"{PlexTvUrl}/pms/servers.xml?X-Plex-Token={token}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-Plex-Token", token);

                var response = await httpClient.SendAsync(request);
                
                System.Diagnostics.Debug.WriteLine($"Server list API URL: {url}");
                System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
                Console.WriteLine($"[DEBUG] Server list API URL: {url}");
                Console.WriteLine($"[DEBUG] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Server list response: {content}");
                    
                    // Try JSON first
                    if (content.TrimStart().StartsWith("{"))
                    {
                        // Parse JSON response
                        dynamic result = JsonConvert.DeserializeObject(content);
                        
                        // Navigate the JSON structure: MediaContainer.Server (array)
                        if (result?.MediaContainer?.Server != null)
                        {
                            var serverArray = result.MediaContainer.Server;
                            
                            // Handle both single server and array of servers
                            if (serverArray is Newtonsoft.Json.Linq.JArray)
                            {
                                foreach (var serverJson in serverArray)
                                {
                                    var server = CreatePlexServerFromJson(serverJson);
                                    if (server != null)
                                    {
                                        servers.Add(server);
                                        System.Diagnostics.Debug.WriteLine($"Added server: {server.Name} at {server.Address}:{server.Port}");
                                    }
                                }
                            }
                            else
                            {
                                // Single server case
                                var server = CreatePlexServerFromJson(serverArray);
                                if (server != null)
                                {
                                    servers.Add(server);
                                    System.Diagnostics.Debug.WriteLine($"Added single server: {server.Name} at {server.Address}:{server.Port}");
                                }
                            }
                        }
                    }
                    else if (content.TrimStart().StartsWith("<"))
                    {
                        // Fall back to XML parsing
                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(content);
                        
                        var serverNodes = xmlDoc.SelectNodes("//Server");
                        if (serverNodes != null)
                        {
                            foreach (XmlNode serverNode in serverNodes)
                            {
                                var server = new PlexServer
                                {
                                    Name = serverNode.Attributes?["name"]?.Value ?? "",
                                    Host = serverNode.Attributes?["host"]?.Value ?? "",
                                    Address = serverNode.Attributes?["address"]?.Value ?? "",
                                    LocalAddresses = serverNode.Attributes?["localAddresses"]?.Value ?? "",
                                    Port = int.TryParse(serverNode.Attributes?["port"]?.Value, out int port) ? port : 32400,
                                    MachineIdentifier = serverNode.Attributes?["machineIdentifier"]?.Value ?? "",
                                    Version = serverNode.Attributes?["version"]?.Value ?? ""
                                };
                                
                                if (!string.IsNullOrEmpty(server.Address))
                                {
                                    servers.Add(server);
                                    var bestAddress = server.GetBestAddress();
                                    var bestPort = server.GetBestPort();
                                    var isLocal = server.IsLocal();
                                    var finalUrl = server.GetUrl();
                                    System.Diagnostics.Debug.WriteLine($"Added server (XML): {server.Name} at {server.Address}:{server.Port} (LocalAddrs: {server.LocalAddresses}) -> Final URL: {finalUrl} (IsLocal: {isLocal})");
                                    Console.WriteLine($"[DEBUG] Added server (XML): {server.Name} at {server.Address}:{server.Port} (LocalAddrs: {server.LocalAddresses}) -> Final URL: {finalUrl} (IsLocal: {isLocal})");
                                }
                            }
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Total servers found: {servers.Count}");
                return servers;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetServerList exception: {ex.Message}");
                throw new Exception($"Failed to get server list: {ex.Message}");
            }
        }

        private PlexServer CreatePlexServerFromJson(dynamic serverJson)
        {
            try
            {
                var server = new PlexServer
                {
                    Name = serverJson?.name?.ToString() ?? "",
                    Host = serverJson?.host?.ToString() ?? "",
                    Address = serverJson?.address?.ToString() ?? "",
                    LocalAddresses = serverJson?.localAddresses?.ToString() ?? "",
                    Port = int.TryParse(serverJson?.port?.ToString(), out int port) ? port : 32400,
                    MachineIdentifier = serverJson?.machineIdentifier?.ToString() ?? "",
                    Version = serverJson?.version?.ToString() ?? ""
                };
                
                return !string.IsNullOrEmpty(server.Address) ? server : null;
            }
            catch
            {
                return null;
            }
        }

        public PlexServer FindBestServer(List<PlexServer> servers)
        {
            System.Diagnostics.Debug.WriteLine($"FindBestServer: Evaluating {servers.Count} servers");
            Console.WriteLine($"[DEBUG] FindBestServer: Evaluating {servers.Count} servers");
            
            foreach (var server in servers)
            {
                var finalUrl = server.GetUrl();
                System.Diagnostics.Debug.WriteLine($"  Server: {server.Name} - Public: {server.Address}:{server.Port}, Local: [{server.LocalAddresses}], Final URL: {finalUrl} - IsLocal: {server.IsLocal()}");
                Console.WriteLine($"[DEBUG]   Server: {server.Name} - Public: {server.Address}:{server.Port}, Local: [{server.LocalAddresses}], Final URL: {finalUrl} - IsLocal: {server.IsLocal()}");
            }
            
            // Priority: Local servers first, preferring 192.168.x.x over others
            var localServers = servers.Where(s => s.IsLocal()).ToList();
            
            if (localServers.Any())
            {
                System.Diagnostics.Debug.WriteLine($"Found {localServers.Count} local servers");
                
                // Prefer 192.168.x.x networks (most common home networks)
                var homeNetworkServers = localServers.Where(s => s.Address.StartsWith("192.168.")).ToList();
                if (homeNetworkServers.Any())
                {
                    var selected = homeNetworkServers.First();
                    System.Diagnostics.Debug.WriteLine($"Selected home network server: {selected.Name} at {selected.Address}:{selected.Port}");
                    Console.WriteLine($"[DEBUG] Selected home network server: {selected.Name} at {selected.Address}:{selected.Port}");
                    return selected;
                }
                
                // Next prefer 10.x.x.x networks
                var tenNetworkServers = localServers.Where(s => s.Address.StartsWith("10.")).ToList();
                if (tenNetworkServers.Any())
                {
                    var selected = tenNetworkServers.First();
                    System.Diagnostics.Debug.WriteLine($"Selected 10.x network server: {selected.Name} at {selected.Address}:{selected.Port}");
                    return selected;
                }
                
                // Finally 172.x.x.x networks
                var selected172 = localServers.First();
                System.Diagnostics.Debug.WriteLine($"Selected local server: {selected172.Name} at {selected172.Address}:{selected172.Port}");
                return selected172;
            }
            
            // If no local servers, return the first available server
            var fallback = servers.FirstOrDefault();
            if (fallback != null)
            {
                System.Diagnostics.Debug.WriteLine($"No local servers found, using fallback: {fallback.Name} at {fallback.Address}:{fallback.Port}");
                Console.WriteLine($"[DEBUG] No local servers found, using fallback: {fallback.Name} at {fallback.Address}:{fallback.Port}");
            }
            return fallback;
        }

        public async Task<List<PlexLibrary>> GetLibraries(string serverUrl, string token, bool forceRefresh = false)
        {
            try
            {
                var cacheKey = $"{serverUrl}_{token}";
                
                // Check cache first (unless force refresh is requested)
                if (!forceRefresh && libraryCache.ContainsKey(cacheKey))
                {
                    var cachedData = libraryCache[cacheKey];
                    if (DateTime.UtcNow - cachedData.CachedAt < CacheExpiration)
                    {
                        // Return cached libraries (create new instances to avoid reference issues)
                        return cachedData.Libraries.ConvertAll(lib => new PlexLibrary
                        {
                            Name = lib.Name,
                            Type = lib.Type,
                            IsSelected = lib.IsSelected
                        });
                    }
                    else
                    {
                        // Cache expired, remove it
                        libraryCache.Remove(cacheKey);
                    }
                }
                
                // Fetch libraries from Plex server
                var libraries = new List<PlexLibrary>();
                var url = $"{serverUrl}/library/sections?X-Plex-Token={token}";

                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var xmlContent = await response.Content.ReadAsStringAsync();
                    var xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);

                    var sections = xmlDoc.SelectNodes("//Directory");
                    if (sections != null)
                    {
                        foreach (XmlNode section in sections)
                        {
                            var library = new PlexLibrary
                            {
                                Name = section.Attributes?["title"]?.Value ?? "",
                                Type = section.Attributes?["type"]?.Value ?? "",
                                IsSelected = false
                            };
                            
                            if (!string.IsNullOrEmpty(library.Name))
                            {
                                libraries.Add(library);
                            }
                        }
                    }
                    
                    // Cache the results
                    libraryCache[cacheKey] = new CachedLibraries
                    {
                        Libraries = libraries,
                        CachedAt = DateTime.UtcNow
                    };
                }

                return libraries;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get libraries: {ex.Message}");
            }
        }

        public async Task<bool> ValidateConnection(string serverUrl, string token)
        {
            try
            {
                var url = $"{serverUrl}/identity?X-Plex-Token={token}";
                var response = await httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public string ExtractServerIpFromUrl(string serverUrl)
        {
            try
            {
                var uri = new Uri(serverUrl);
                return uri.Host;
            }
            catch
            {
                return "192.168.1.12"; // Default fallback
            }
        }

        public string FormatServerUrl(string ipAddress, int port = 32400)
        {
            if (!ipAddress.StartsWith("http"))
            {
                return $"http://{ipAddress}:{port}";
            }
            return ipAddress;
        }
        
        /// <summary>
        /// Clears the library cache for all servers or a specific server
        /// </summary>
        /// <param name="serverUrl">Optional: Clear cache only for specific server</param>
        /// <param name="token">Optional: Clear cache only for specific server with token</param>
        public static void ClearCache(string serverUrl = null, string token = null)
        {
            if (!string.IsNullOrEmpty(serverUrl) && !string.IsNullOrEmpty(token))
            {
                var cacheKey = $"{serverUrl}_{token}";
                libraryCache.Remove(cacheKey);
            }
            else
            {
                libraryCache.Clear();
            }
        }
        
        /// <summary>
        /// Gets information about cached libraries
        /// </summary>
        /// <returns>Dictionary with cache info</returns>
        public static Dictionary<string, DateTime> GetCacheInfo()
        {
            var cacheInfo = new Dictionary<string, DateTime>();
            foreach (var cache in libraryCache)
            {
                cacheInfo[cache.Key] = cache.Value.CachedAt;
            }
            return cacheInfo;
        }
    }
    
    /// <summary>
    /// Internal class to store cached library data
    /// </summary>
    internal class CachedLibraries
    {
        public List<PlexLibrary> Libraries { get; set; }
        public DateTime CachedAt { get; set; }
    }
}