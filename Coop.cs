using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Controls local cooperative multiplayer, player slots, connected gamepads, and player 2 states.
    /// </summary>
    public class CoopController
    {
        private CoopManager Manager
        {
            get
            {
                var inst = CoopManager.Instance;
                return Romby.IsValid(inst) ? inst : null;
            }
        }

        /// <summary>Checks whether cooperative multiplayer mode has active joined players.</summary>
        public bool IsCoopActive
        {
            get
            {
                var mgr = Manager;
                return mgr != null && mgr.CoopPlayerCount > 0;
            }
        }

        /// <summary>Total number of joined cooperative secondary players.</summary>
        public int PlayerCount
        {
            get
            {
                var mgr = Manager;
                return mgr != null ? mgr.CoopPlayerCount : 0;
            }
        }

        /// <summary>Gets a snapshot list of all currently active cooperative players.</summary>
        public IReadOnlyList<CoopPlayerController> ActivePlayers
        {
            get
            {
                var players = CoopManager.ActiveCoopPlayers;
                if (players != null)
                {
                    var result = new List<CoopPlayerController>();
                    for (int i = 0; i < players.Length; i++)
                    {
                        var ctrl = players[i];
                        if (Romby.IsValid(ctrl)) result.Add(ctrl);
                    }
                    return result;
                }
                return Array.Empty<CoopPlayerController>();
            }
        }

        /// <summary>Gets a joined cooperative player controller by slot index (0-based).</summary>
        public CoopPlayerController GetPlayer(int playerIndex)
        {
            var mgr = Manager;
            if (mgr != null)
            {
                var ctrl = CoopManager.GetActiveCoopPlayerByIndex(playerIndex);
                return Romby.IsValid(ctrl) ? ctrl : null;
            }
            return null;
        }

        /// <summary>Checks if a player slot has a joined cooperative player.</summary>
        public bool IsPlayerJoined(int playerIndex)
        {
            var mgr = Manager;
            return mgr != null && mgr.IsCoopPlayerJoined(playerIndex);
        }

        /// <summary>Disconnects and removes a cooperative player from the session.</summary>
        public bool RemovePlayer(int playerIndex)
        {
            var mgr = Manager;
            return mgr != null && mgr.RemoveCoopPlayerByIndex(playerIndex);
        }

        /// <summary>Toggles cooperative player status and gamepad assignment HUD panel.</summary>
        public void ToggleStatusPanel()
        {
            var mgr = Manager;
            if (mgr != null) mgr.ToggleCoopStatusPanel();
        }
    }
}
