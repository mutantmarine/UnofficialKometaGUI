using Microsoft.AspNetCore.Mvc;
using KometaGUIv3.Web.Services;
using KometaGUIv3.Shared.Services;
using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlexController : ControllerBase
    {
        private readonly PlexService _plexService;
        private readonly ProfileManager _profileManager;

        public PlexController(PlexService plexService, ProfileManager profileManager)
        {
            _plexService = plexService;
            _profileManager = profileManager;
        }

        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] PlexAuthRequest request)
        {
            try
            {
                var token = await _plexService.AuthenticateUser(request.Email, request.Password);
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { error = "Authentication failed. Please check your credentials." });
                }

                // Get servers for this user
                var servers = await _plexService.GetServerList(token);
                var bestServer = _plexService.FindBestServer(servers);
                
                string serverUrl = bestServer?.GetUrl() ?? "http://192.168.1.100:32400";

                // Get libraries from the server
                var libraries = await _plexService.GetLibraries(serverUrl, token);

                return Ok(new
                {
                    token = token,
                    serverUrl = serverUrl,
                    serverName = bestServer?.Name ?? "Plex Server",
                    libraries = libraries.Select(l => new
                    {
                        name = l.Name,
                        type = l.Type
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Authentication error: {ex.Message}" });
            }
        }

        [HttpGet("libraries")]
        public async Task<IActionResult> GetLibraries([FromQuery] string serverUrl, [FromQuery] string token)
        {
            try
            {
                var libraries = await _plexService.GetLibraries(serverUrl, token);
                return Ok(libraries.Select(l => new
                {
                    name = l.Name,
                    type = l.Type
                }).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error fetching libraries: {ex.Message}" });
            }
        }

        [HttpPost("validate-server")]
        public async Task<IActionResult> ValidateServer([FromBody] ServerValidationRequest request)
        {
            try
            {
                var libraries = await _plexService.GetLibraries(request.ServerUrl, request.Token);
                return Ok(new
                {
                    isValid = libraries.Any(),
                    libraryCount = libraries.Count,
                    libraries = libraries.Select(l => new
                    {
                        name = l.Name,
                        type = l.Type
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Server validation failed: {ex.Message}" });
            }
        }
    }

    public class PlexAuthRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ProfileName { get; set; }
    }

    public class ServerValidationRequest
    {
        public string ServerUrl { get; set; }
        public string Token { get; set; }
    }
}