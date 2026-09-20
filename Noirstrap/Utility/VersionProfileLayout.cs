namespace Noirstrap.Utility
{
    // Replaces the v420.24 junction layout, which launched every client through an mklink /J reparse
    // point at Versions\version-<hash>\ pointing to Versions\profile-<id>\.
    //
    // Why it changed (2026-07-19): a user's diagnostics bundle contained a same-machine control —
    // one Fishstrap session and three ExploitStrap sessions, same build, same hour. Fishstrap's
    // client disconnected cleanly at 92s; all three of ours died 41-58s in while still connected to
    // the server, with the log truncating mid-gameplay and no shutdown sequence. That is Hyperion's
    // signature, and the reparse point in the launch path was the only structural difference.
    //
    // The layout now has no junction at all:
    //
    //     Versions\version-<hash>\   real directory — the ACTIVE profile's install
    //     Versions\profile-<id>\     real directories — every inactive profile's install, parked
    //
    // Switching profiles is two same-volume renames (park the outgoing, unpark the incoming), which
    // NTFS does atomically and instantly regardless of how many gigabytes are inside. Three things
    // fall out of that:
    //
    //   * nothing is launched through a reparse point, which is the crash fix;
    //   * both the PEB path and the NTFS-resolved path read ...\Versions\version-<hash>\, so tools
    //     that identify the build from the folder name work whichever API they use;
    //   * the path gets SHORTER. Roblox's deepest content files sit ~170 characters below the
    //     version folder, and a custom profile's real path (Versions\profile-<36-char-guid>\) was
    //     already 266 characters against Win32's 260 limit — those files have been failing to
    //     extract silently. Win32 enforces the limit on the string you pass, not the one it
    //     resolves to, which is how the junction hid it. Versions\version-<hash>\ is 246.
    //
    // Per-profile isolation is preserved: only one profile is ever unparked, so two profiles pinned
    // to the same hash still cannot share a folder.
    public static class VersionProfileLayout
    {
        private const string LOG_IDENT = "VersionProfileLayout";

        public static string ParkedPath(string profileId) => Path.Combine(Paths.Versions, "profile-" + profileId);

        public static string ActivePath(string versionGuid) => Path.Combine(Paths.Versions, versionGuid);

        /// <summary>
        /// Makes <paramref name="profile"/>'s install the one living at Versions\<paramref name="versionGuid"/>\,
        /// parking whichever profile was there before. Returns the directory the client should launch
        /// from — the active path on success, or the profile's own parked path if the shuffle couldn't
        /// be completed, so a launch never hard-fails over this.
        /// </summary>
        /// <remarks>
        /// The ORDER below is the whole point, and it used to be wrong.
        ///
        /// Parking only happened inside <c>if (Directory.Exists(active))</c> — that is, only when the
        /// INCOMING profile's version directory was already occupied. Switching to a profile pinned to
        /// a hash that wasn't on disk yet therefore left the outgoing profile unparked, while
        /// <see cref="RecordOwner"/> still overwrote State with the incoming profile. One switch later,
        /// that stale owner was used to decide where the occupant belonged, and an entire install got
        /// moved into a different profile's parked folder.
        ///
        /// Two launches after that, the pinned profile ends up launching the current LIVE client out of
        /// its own directory while every log line and the Versions Manager report the pinned build —
        /// the exact executor-compatibility failure the profile system exists to prevent. Nothing
        /// catches it either: Bootstrapper's Error-280 mismatch check is gated on
        /// <c>_latestVersion is not null</c>, and that stays null for pinned profiles.
        ///
        /// So park by what State RECORDS, before looking at the incoming path at all.
        /// </remarks>
        public static string EnsureActive(VersionProfile profile, string versionGuid)
        {
            string active = ActivePath(versionGuid);
            string parked = ParkedPath(profile.Id);

            try
            {
                Directory.CreateDirectory(Paths.Versions);

                RemoveLegacyJunction(active);

                // Already unparked and ours at this exact hash? Nothing to do — the common case on
                // repeat launches of the same profile.
                if (App.State.Prop.ActiveInstallProfileId == profile.Id
                    && App.State.Prop.ActiveInstallVersionGuid == versionGuid
                    && Directory.Exists(active)
                    && !VersionJunctionManager.IsJunction(active))
                {
                    return active;
                }

                // 1. Park whoever State says is currently unparked. Nothing about this depends on the
                //    incoming path, which is what the old code got wrong.
                //
                //    On failure we return our OWN parked path rather than the active one: the thing we
                //    couldn't move is another profile's live install, and if both profiles happen to
                //    sit on the same hash then `active` IS that install. Handing it back would launch
                //    us out of their folder and let the downloader write into it.
                if (!ParkRecordedOccupant(profile.Id))
                    return parked;

                // 2. Anything still sitting at the incoming path is unaccounted for by State.
                if (Directory.Exists(active) && !ResolveStrayOccupant(active, versionGuid, profile))
                    return parked;

                // 3. Unpark ours, or leave the path free for the downloader to populate. Skipped
                //    entirely when step 2 adopted a directory in place.
                if (!Directory.Exists(active))
                {
                    if (Directory.Exists(parked))
                    {
                        Directory.Move(parked, active);
                        App.Logger.WriteLine(LOG_IDENT, $"Unparked '{profile.Name}' → {Path.GetFileName(active)}");
                    }
                    else
                    {
                        Directory.CreateDirectory(active);
                    }
                }

                RecordOwner(profile.Id, versionGuid);
                return active;
            }
            catch (Exception ex)
            {
                // Never let a layout problem stop a launch. Falling back to the parked path costs us
                // the short-path and folder-name benefits but still runs.
                App.Logger.WriteException(LOG_IDENT + "::EnsureActive", ex);
                return Directory.Exists(parked) ? parked : active;
            }
        }

        /// <summary>
        /// Moves the install State records as currently unparked back into its owner's parked folder.
        /// Returns false when it couldn't be moved — almost always a client still running out of it —
        /// so the caller backs off rather than clobbering a live install.
        /// </summary>
        private static bool ParkRecordedOccupant(string incomingProfileId)
        {
            string ownerId = App.State.Prop.ActiveInstallProfileId;
            string ownerGuid = App.State.Prop.ActiveInstallVersionGuid;

            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(ownerGuid))
                return true; // nothing is recorded as unparked

            // Same profile moving to a different hash, i.e. a Roblox version bump. Leave the old
            // directory where it is: installs are version-specific, CleanupVersionsFolder retires it
            // once the upgrade stamps the new InstalledVersionGuid, and parking then immediately
            // unparking it would hand the new hash a folder full of the PREVIOUS build's files —
            // which the background updater would happily extract over, since it skips the clean-wipe.
            if (string.Equals(ownerId, incomingProfileId, StringComparison.OrdinalIgnoreCase))
                return true;

            string ownerActive = ActivePath(ownerGuid);

            // State is stale (interrupted switch, or the user deleted the folder). Nothing to move,
            // and the RecordOwner at the end of EnsureActive corrects the record.
            if (!Directory.Exists(ownerActive))
                return true;

            return TryMoveAside(ownerActive, ParkedPath(ownerId), "Parked");
        }

        /// <summary>
        /// Handles a directory sitting at the incoming profile's version path that State doesn't
        /// account for — a pre-migration install, a Studio directory, or the leftovers of an
        /// interrupted switch. Returns false if it couldn't be cleared out of the way.
        /// </summary>
        private static bool ResolveStrayOccupant(string active, string versionGuid, VersionProfile incoming)
        {
            // Ownership is decided by which profile records this hash as its install — deliberately
            // NOT by State.ActiveInstallProfileId. That field describes the profile step 1 just
            // parked, and trusting it here is exactly what used to move one profile's install into
            // another profile's folder.
            var claimant = App.Settings.Prop.VersionProfiles.FirstOrDefault(p =>
                string.Equals(p.InstalledVersionGuid, versionGuid, StringComparison.OrdinalIgnoreCase));

            if (claimant is not null && !string.Equals(claimant.Id, incoming.Id, StringComparison.OrdinalIgnoreCase))
            {
                App.Logger.WriteLine(LOG_IDENT, $"{Path.GetFileName(active)} is claimed by profile '{claimant.Name}' — parking it there first.");
                return TryMoveAside(active, ParkedPath(claimant.Id), "Parked");
            }

            if (!Directory.Exists(ParkedPath(incoming.Id)))
            {
                // Ours, or nobody's, and we have no parked copy to prefer — take it as-is rather than
                // moving it out and straight back in again.
                App.Logger.WriteLine(LOG_IDENT, $"{Path.GetFileName(active)} has no other claimant — adopting it in place for '{incoming.Name}'.");
                return true;
            }

            // We have BOTH a parked copy and something unaccounted-for at the active path. Keep the
            // parked copy (it's the one that carries our profile's name) and set the stray aside
            // rather than deleting anything — CleanupVersionsFolder skips dot-prefixed names, so it
            // survives for the user to inspect.
            return TryMoveAside(active, StalePath(Path.GetFileName(active)), "Set aside an unaccounted-for install");
        }

        /// <summary>
        /// Same-volume directory rename, which NTFS does atomically and instantly at any size. Fails
        /// when a client is still running out of the directory, and that failure is the signal to back
        /// off rather than clobber a live install.
        /// </summary>
        private static bool TryMoveAside(string source, string destination, string verb)
        {
            try
            {
                if (Directory.Exists(destination))
                {
                    // The owner somehow has both a parked and an unparked copy. The unparked one is
                    // the live install, so keep it and set the duplicate aside.
                    string aside = StalePath(Path.GetFileName(destination));
                    Directory.Move(destination, aside);
                    App.Logger.WriteLine(LOG_IDENT, $"Set aside a duplicate parked install as {Path.GetFileName(aside)}");
                }

                Directory.Move(source, destination);
                App.Logger.WriteLine(LOG_IDENT, $"{verb} {Path.GetFileName(source)} → {Path.GetFileName(destination)}");
                return true;
            }
            catch (IOException ex)
            {
                // Locked, which in practice means a client is still running from it.
                App.Logger.WriteLine(LOG_IDENT, $"Couldn't move {Path.GetFileName(source)} — it's in use. Close Roblox and relaunch to switch profiles.");
                App.Logger.WriteException(LOG_IDENT + "::TryMoveAside", ex);
                return false;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException(LOG_IDENT + "::TryMoveAside", ex);
                return false;
            }
        }

        private static string StalePath(string name) =>
            Path.Combine(Paths.Versions, $".stale-{name}-{DateTime.UtcNow:yyyyMMddTHHmmssZ}");

        /// <summary>
        /// v420.24-era junctions are unlinked on sight. Directory.Delete on a reparse point removes
        /// only the link, so the profile directory it pointed at survives — already in exactly the
        /// parked shape this layout expects, which is what makes the migration free.
        /// </summary>
        private static void RemoveLegacyJunction(string path)
        {
            if (!VersionJunctionManager.IsJunction(path))
                return;

            if (VersionJunctionManager.DeleteJunction(path))
                App.Logger.WriteLine(LOG_IDENT, $"Removed legacy junction {Path.GetFileName(path)} — the client no longer launches through a reparse point.");
        }

        private static void RecordOwner(string profileId, string versionGuid)
        {
            if (App.State.Prop.ActiveInstallProfileId == profileId
                && App.State.Prop.ActiveInstallVersionGuid == versionGuid)
            {
                return;
            }

            App.State.Prop.ActiveInstallProfileId = profileId;
            App.State.Prop.ActiveInstallVersionGuid = versionGuid;
            App.State.Save();
        }

        /// <summary>
        /// True when this profile's install is the unparked one. Replaces the old junction-target
        /// check behind the Versions Manager's "install target" badge: with no junctions, being
        /// unparked is exactly what makes a profile the target an executor installer writes into.
        /// </summary>
        public static bool IsInstallTarget(string profileId) =>
            !string.IsNullOrEmpty(profileId) && App.State.Prop.ActiveInstallProfileId == profileId;
    }
}

