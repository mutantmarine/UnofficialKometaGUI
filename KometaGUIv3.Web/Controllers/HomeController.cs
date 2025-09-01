using Microsoft.AspNetCore.Mvc;
using KometaGUIv3.Shared.Services;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Web.Models;

namespace KometaGUIv3.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ProfileManager _profileManager;
        private readonly YamlGenerator _yamlGenerator;

        public HomeController(ProfileManager profileManager, YamlGenerator yamlGenerator)
        {
            _profileManager = profileManager;
            _yamlGenerator = yamlGenerator;
        }

        public IActionResult Welcome()
        {
            return View();
        }

        public IActionResult ProfileManagement()
        {
            var profiles = _profileManager.GetAllProfiles();
            var model = new ProfileManagementViewModel
            {
                Profiles = profiles,
                SelectedProfile = null
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult CreateProfile(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                TempData["Error"] = "Profile name is required.";
                return RedirectToAction("ProfileManagement");
            }

            try
            {
                var profile = _profileManager.CreateProfile(profileName);
                TempData["Success"] = $"Profile '{profileName}' created successfully.";
                return RedirectToAction("ProfileManagement");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating profile: {ex.Message}";
                return RedirectToAction("ProfileManagement");
            }
        }

        [HttpPost]
        public IActionResult DeleteProfile(string profileName)
        {
            try
            {
                _profileManager.DeleteProfile(profileName);
                TempData["Success"] = $"Profile '{profileName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting profile: {ex.Message}";
            }
            return RedirectToAction("ProfileManagement");
        }

        public IActionResult Connections(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                TempData["Error"] = "Please select a profile first.";
                return RedirectToAction("ProfileManagement");
            }

            var profile = _profileManager.LoadProfile(profileName);
            if (profile == null)
            {
                TempData["Error"] = $"Profile '{profileName}' not found.";
                return RedirectToAction("ProfileManagement");
            }

            var model = new ConnectionsViewModel
            {
                Profile = profile
            };

            return View(model);
        }

        public IActionResult Charts(string profileName = null)
        {
            // Allow access without profile for web interface navigation
            return View();
        }

        public IActionResult Collections(string profileName = null)
        {
            // Allow access without profile for web interface navigation (like Charts)
            // but provide data if profile is specified
            if (!string.IsNullOrEmpty(profileName))
            {
                var profile = _profileManager.LoadProfile(profileName);
                if (profile == null)
                {
                    TempData["Error"] = $"Profile '{profileName}' not found.";
                    return RedirectToAction("ProfileManagement");
                }

                var model = new CollectionsViewModel
                {
                    Profile = profile,
                    ChartCollections = KometaDefaults.ChartCollections,
                    AwardCollections = KometaDefaults.AwardCollections,
                    MovieCollections = KometaDefaults.MovieCollections,
                    ShowCollections = KometaDefaults.ShowCollections,
                    BothCollections = KometaDefaults.BothCollections
                };

                return View(model);
            }

            // For now, redirect to Charts view as it has the updated Collections interface
            return RedirectToAction("Charts");
        }

        public IActionResult Overlays(string profileName = null)
        {
            // Allow access without profile for web interface navigation
            return View();
        }

        public IActionResult Services(string profileName = null)
        {
            // Allow access without profile for web interface navigation
            return View();
        }

        public IActionResult OptionalServices(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                TempData["Error"] = "Please select a profile first.";
                return RedirectToAction("ProfileManagement");
            }

            var profile = _profileManager.LoadProfile(profileName);
            if (profile == null)
            {
                TempData["Error"] = $"Profile '{profileName}' not found.";
                return RedirectToAction("ProfileManagement");
            }

            var model = new OptionalServicesViewModel
            {
                Profile = profile
            };

            return View(model);
        }

        public IActionResult Settings(string profileName = null)
        {
            // Allow access without profile for web interface navigation
            return View();
        }

        public IActionResult Actions(string profileName = null)
        {
            // Allow access without profile for web interface navigation
            return View();
        }

        public IActionResult FinalActions(string profileName)
        {
            if (string.IsNullOrEmpty(profileName))
            {
                TempData["Error"] = "Please select a profile first.";
                return RedirectToAction("ProfileManagement");
            }

            var profile = _profileManager.LoadProfile(profileName);
            if (profile == null)
            {
                TempData["Error"] = $"Profile '{profileName}' not found.";
                return RedirectToAction("ProfileManagement");
            }

            var model = new FinalActionsViewModel
            {
                Profile = profile
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult GenerateYaml(string profileName)
        {
            try
            {
                var profile = _profileManager.LoadProfile(profileName);
                if (profile == null)
                {
                    return Json(new { success = false, error = $"Profile '{profileName}' not found." });
                }

                var yamlContent = _yamlGenerator.GenerateKometaConfig(profile);
                var fileName = $"{profile.Name}_config.yml";

                return Json(new { 
                    success = true, 
                    content = yamlContent, 
                    filename = fileName 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}