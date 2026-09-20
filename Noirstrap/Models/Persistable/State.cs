using System.Windows.Forms;

namespace Noirstrap.Models.Persistable
{
    public class State
    {
        public bool TestModeWarningShown { get; set; } = false;

        public bool IgnoreOutdatedChannel { get; set; } = false;

        public bool WatcherRunning { get; set; } = false;

        public bool PromptWebView2Install { get; set; } = true;

        public string? LastPage {  get; set; } = null!;

        public bool ForceReinstall { get; set; } = false;

        public DateTime LastUpdateCheckTime { get; set; } = DateTime.MinValue;

        public WindowState SettingsWindow { get; set; } = new();

        // ===== ExploitStrap port: Versions Manager install tracking =====
        public string ActiveInstallProfileId { get; set; } = "";
        public string ActiveInstallVersionGuid { get; set; } = "";
        public List<string> RecentCustomVersionHashes { get; set; } = new();
        public List<RecentPlace> RecentPlaces { get; set; } = new();
        public string DismissedLiveHash { get; set; } = "";
        public Dictionary<string, DateTime> LiveHashFirstSeenUtc { get; set; } = new();
        public string LastNotifiedLiveHash { get; set; } = "";
        public string LastNotifiedAppVersion { get; set; } = "";
        public int UpdateCheckFailureStreak { get; set; } = 0;
        public DateTime? LastSuccessfulUpdateCheckUtc { get; set; } = null;
        public string LastNotifiedDeadUpdaterVersion { get; set; } = "";

        public void PushRecentPlace(long placeId, string? name)
        {
            if (placeId <= 0)
                return;

            var existing = RecentPlaces.FirstOrDefault(p => p.PlaceId == placeId);
            RecentPlaces.RemoveAll(p => p.PlaceId == placeId);

            RecentPlaces.Insert(0, new RecentPlace
            {
                PlaceId = placeId,
                Name = String.IsNullOrWhiteSpace(name) ? existing?.Name ?? "" : name,
                LastPlayedUtc = DateTime.UtcNow,
            });

            while (RecentPlaces.Count > 10)
                RecentPlaces.RemoveAt(RecentPlaces.Count - 1);
        }

        #region Deprecated properties
        /// <summary>
        /// Deprecated, use App.RobloxState.Player
        /// </summary>
        public AppState? Player { private get; set; }
        public AppState? GetDeprecatedPlayer() => Player;

        /// <summary>
        /// Deprecated, use App.RobloxState.Studio
        /// </summary>
        public AppState? Studio { private get; set; }
        public AppState? GetDeprecatedStudio() => Studio;

        /// <summary>
        /// Deprecated, use App.RobloxState.ModManifest
        /// </summary>
        public List<string>? ModManifest { private get; set; }
        public List<string>? GetDeprecatedModManifest() => ModManifest;
        #endregion
    }
}
