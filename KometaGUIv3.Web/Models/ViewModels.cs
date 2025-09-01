using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Web.Models
{
    public class ProfileManagementViewModel
    {
        public List<KometaProfile> Profiles { get; set; } = new List<KometaProfile>();
        public KometaProfile? SelectedProfile { get; set; }
    }

    public class ConnectionsViewModel
    {
        public KometaProfile Profile { get; set; } = new KometaProfile();
    }

    public class CollectionsViewModel
    {
        public KometaProfile Profile { get; set; } = new KometaProfile();
        public Dictionary<string, List<DefaultCollection>> ChartCollections { get; set; } = new Dictionary<string, List<DefaultCollection>>();
        public Dictionary<string, List<DefaultCollection>> AwardCollections { get; set; } = new Dictionary<string, List<DefaultCollection>>();
        public Dictionary<string, List<DefaultCollection>> MovieCollections { get; set; } = new Dictionary<string, List<DefaultCollection>>();
        public Dictionary<string, List<DefaultCollection>> ShowCollections { get; set; } = new Dictionary<string, List<DefaultCollection>>();
        public Dictionary<string, List<DefaultCollection>> BothCollections { get; set; } = new Dictionary<string, List<DefaultCollection>>();
    }

    public class OverlaysViewModel
    {
        public KometaProfile Profile { get; set; } = new KometaProfile();
        public Dictionary<string, OverlayInfo> AllOverlays { get; set; } = new Dictionary<string, OverlayInfo>();
        public Dictionary<string, string[]> MediaTypeOverlays { get; set; } = new Dictionary<string, string[]>();
        public Dictionary<int, string> PositionDescriptions { get; set; } = new Dictionary<int, string>();
    }

    public class OptionalServicesViewModel
    {
        public KometaProfile Profile { get; set; } = new KometaProfile();
    }

    public class SettingsViewModel
    {
        public KometaProfile Profile { get; set; } = new KometaProfile();
    }

    public class FinalActionsViewModel
    {
        public KometaProfile Profile { get; set; } = new KometaProfile();
    }
}