using System;
using UnityEngine;
using Il2CppCollections = Il2CppSystem.Collections.Generic;

namespace RombyLib
{
    /// <summary>
    /// Controller for discovering and interacting with hub and exploration NPCs.
    /// </summary>
    public class NPCController
    {
        private NPCCientifico _scientist;
        private NPCEscultor _sculptor;
        private NPCModista _tailor;
        private NPCTarotista _fortuneTeller;

        internal NPCController() { }

        private static T GetCached<T>(ref T cached) where T : UnityEngine.Object
        {
            if (!Romby.IsValid(cached))
            {
                try
                {
                    var found = UnityEngine.Object.FindObjectOfType<T>();
                    cached = Romby.IsValid(found) ? found : null;
                }
                catch
                {
                    cached = null;
                }
            }
            return Romby.IsValid(cached) ? cached : null;
        }

        /// <summary>Finds all active NPC interactable instances in the scene.</summary>
        public NPCInteractable[] All
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<NPCInteractable>() ?? Array.Empty<NPCInteractable>(); }
                catch { return Array.Empty<NPCInteractable>(); }
            }
        }

        /// <summary>Gets the total count of active NPCs in the scene.</summary>
        public int Count => All.Length;

        /// <summary>Checks whether any NPC interaction or dialogue panel is currently open.</summary>
        public bool IsAnyMenuOpen
        {
            get
            {
                try { return NPCInteractable.IsAnyNPCPanelOpen; }
                catch { return false; }
            }
        }

        /// <summary>Finds an active NPC by name or localized character name.</summary>
        public NPCInteractable Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var list = All;
            for (int i = 0; i < list.Length; i++)
            {
                var npc = list[i];
                if (!Romby.IsValid(npc)) continue;
                if (npc.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return npc;
                if (npc.npcData != null && !string.IsNullOrEmpty(npc.npcData.npcName) &&
                    npc.npcData.npcName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return npc;
            }
            return null;
        }

        /// <summary>Closes any currently open NPC dialogue or interaction interfaces.</summary>
        public void CloseAllMenus()
        {
            var list = All;
            for (int i = 0; i < list.Length; i++)
            {
                var npc = list[i];
                if (Romby.IsValid(npc))
                {
                    try { npc.CloseNPCMenu(); }
                    catch { }
                }
            }
            Debug.Log("[RombyLib.NPC] Closed all active NPC menus.");
        }

        #region Specialized Hub NPCs
        /// <summary>Gets the active Scientist NPC (upgrades, skill reset, infection inversion).</summary>
        public NPCCientifico Scientist => GetCached(ref _scientist);

        /// <summary>Gets the active Sculptor NPC (stone carving and selling).</summary>
        public NPCEscultor Sculptor => GetCached(ref _sculptor);

        /// <summary>Gets the active Tailor NPC (outfits, wardrobe, and workshop panel).</summary>
        public NPCModista Tailor => GetCached(ref _tailor);

        /// <summary>Gets the active Tarot Fortune Teller NPC.</summary>
        public NPCTarotista FortuneTeller => GetCached(ref _fortuneTeller);

        /// <summary>Current infection inversion percentage on the Scientist NPC.</summary>
        public float ScientistInvertedPercentage => Romby.IsValid(Scientist) ? Scientist.GetCurrentInvertedPercentageFloat() : 0f;

        /// <summary>Whether Scientist inversion upgrades have reached maximum capacity.</summary>
        public bool ScientistIsAtMaxInverted => Romby.IsValid(Scientist) && Scientist.IsAtMaxInverted();

        /// <summary>Current cost in currency to reset player skill points at the Scientist.</summary>
        public int ScientistSkillResetCost => Romby.IsValid(Scientist) ? Scientist.GetResetSkillsCost() : 0;

        /// <summary>Price per carved stone given by the Sculptor.</summary>
        public int SculptorPricePerRock
        {
            get => Romby.IsValid(Sculptor) ? Sculptor.dineroPorPiedra : 0;
            set { if (Romby.IsValid(Sculptor)) Sculptor.dineroPorPiedra = value; }
        }

        /// <summary>Opens the rock carving interface at the Sculptor.</summary>
        public void OpenSculptorCarveMenu()
        {
            if (Romby.IsValid(Sculptor) && Romby.IsValid(Sculptor.panelTallarObject))
                Sculptor.panelTallarObject.SetActive(true);
        }

        /// <summary>Opens the rock selling interface at the Sculptor.</summary>
        public void OpenSculptorSellMenu()
        {
            if (Romby.IsValid(Sculptor) && Romby.IsValid(Sculptor.panelVenderRocasObject))
                Sculptor.panelVenderRocasObject.SetActive(true);
        }

        /// <summary>Opens the cosmetic wardrobe interface at the Tailor.</summary>
        public void OpenTailorWardrobe()
        {
            if (Romby.IsValid(Tailor) && Romby.IsValid(Tailor.wardrobeObject))
                Tailor.wardrobeObject.SetActive(true);
        }

        /// <summary>Closes the cosmetic wardrobe interface at the Tailor.</summary>
        public void CloseTailorWardrobe()
        {
            if (Romby.IsValid(Tailor))
                Tailor.CerrarVestidor();
        }

        /// <summary>Opens the Steam Workshop skin panel at the Tailor.</summary>
        public void OpenTailorWorkshopPanel()
        {
            if (Romby.IsValid(Tailor) && Romby.IsValid(Tailor.workshopPanelObject))
                Tailor.workshopPanelObject.SetActive(true);
        }

        /// <summary>Closes the Steam Workshop skin panel at the Tailor.</summary>
        public void CloseTailorWorkshopPanel()
        {
            if (Romby.IsValid(Tailor))
                Tailor.CerrarPanelWorkshop();
        }

        /// <summary>Gets or sets tarot card draw cost at the Fortune Teller.</summary>
        public int TarotCardCost
        {
            get => Romby.IsValid(FortuneTeller) ? FortuneTeller.tarotCardCost : 0;
            set { if (Romby.IsValid(FortuneTeller)) FortuneTeller.tarotCardCost = value; }
        }

        /// <summary>Gets or sets future telling cost at the Fortune Teller.</summary>
        public int FortuneCost
        {
            get => Romby.IsValid(FortuneTeller) ? FortuneTeller.futureCost : 0;
            set { if (Romby.IsValid(FortuneTeller)) FortuneTeller.futureCost = value; }
        }

        /// <summary>Whether the Fortune Teller is currently reading the player's future.</summary>
        public bool IsReadingFuture => Romby.IsValid(FortuneTeller) && Romby.IsValid(FortuneTeller.objetoAActivar) && FortuneTeller.objetoAActivar.activeInHierarchy;
        #endregion

        #region Exploration & Frozen NPCs
        /// <summary>Finds all ice cube blocks trapping frozen NPCs in the scene.</summary>
        public CuboHieloNPC[] FrozenNPCs
        {
            get
            {
                try { return UnityEngine.Object.FindObjectsOfType<CuboHieloNPC>() ?? Array.Empty<CuboHieloNPC>(); }
                catch { return Array.Empty<CuboHieloNPC>(); }
            }
        }

        /// <summary>Total count of frozen NPC ice blocks currently in the world.</summary>
        public int FrozenNPCsCount => FrozenNPCs.Length;

        /// <summary>Breaks all frozen ice blocks in the scene, freeing trapped NPCs immediately.</summary>
        public void BreakAllFrozenNPCs()
        {
            var blocks = FrozenNPCs;
            if (blocks == null || blocks.Length == 0) return;

            int freed = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                var b = blocks[i];
                if (Romby.IsValid(b) && b.IsAlive())
                {
                    try
                    {
                        b.DesbloquearHielo();
                        freed++;
                    }
                    catch { }
                }
            }
            Debug.Log($"[RombyLib.NPC] Broke {freed} frozen NPC ice blocks.");
        }
        #endregion

        /// <summary>Monster ranch/farm manager wrapper.</summary>
        public RanchController Ranch { get; } = new RanchController();

        /// <summary>NPC story messages, rescue sequences, and progression dialogue controller.</summary>
        public NPCStoryController Story { get; } = new NPCStoryController();
    }

    /// <summary>
    /// Controls monster ranch storage, wandering capacity, and entrance/exit transitions.
    /// </summary>
    public class RanchController
    {
        internal RanchController() { }

        private GranjaManager Manager => Romby.IsValid(GranjaManager.Instance) ? GranjaManager.Instance : null;

        /// <summary>Whether players are currently inside the monster ranch area.</summary>
        public bool IsInRanch => Manager != null && Manager.JugadoresEnGranja;

        /// <summary>Total count of captured monsters currently stored in the ranch.</summary>
        public int StoredCount => Manager != null ? Manager.GetAlmacenadosCount() : 0;

        /// <summary>Maximum number of monsters allowed to wander simultaneously in the ranch pen.</summary>
        public int MaxWanderingMonsters
        {
            get => Manager != null ? Manager.maxPaseantes : 0;
            set { if (Manager != null) Manager.maxPaseantes = value; }
        }

        /// <summary>List of all stored captured monster data in the ranch.</summary>
        public Il2CppCollections.List<CapturedMonsterData> StoredMonsters =>
            Manager != null ? Manager.GetAlmacenados() : null;

        /// <summary>Teleports the player into the monster ranch area.</summary>
        public void Enter()
        {
            if (Manager != null)
            {
                Manager.EntrarALaGranja();
                Debug.Log("[RombyLib.Ranch] Entered monster ranch.");
            }
        }

        /// <summary>Teleports the player out of the monster ranch back to the main world.</summary>
        public void Exit()
        {
            if (Manager != null)
            {
                Manager.SalirDeLaGranja();
                Debug.Log("[RombyLib.Ranch] Exited monster ranch.");
            }
        }

        /// <summary>Adds a captured monster to ranch storage.</summary>
        public void AddMonster(CapturedMonsterData data)
        {
            if (Manager != null && data != null)
            {
                Manager.AddToStorage(data);
                Debug.Log("[RombyLib.Ranch] Added monster to ranch storage.");
            }
        }

        /// <summary>Removes a captured monster from ranch storage.</summary>
        public bool RemoveMonster(CapturedMonsterData data)
        {
            if (Manager != null && data != null)
                return Manager.RemoveFromStorage(data);
            return false;
        }
    }

    /// <summary>
    /// Triggers and checks NPC story events, rescue sequences, and boss victory announcements.
    /// </summary>
    public class NPCStoryController
    {
        internal NPCStoryController() { }

        private ManagerMensajesNPCs Manager => Romby.IsValid(ManagerMensajesNPCs.Instance) ? ManagerMensajesNPCs.Instance : null;

        /// <summary>Triggers the Scientist rescue distress call sequence.</summary>
        public void TriggerScientistCall()
        {
            if (Manager != null) Manager.OnAuxilioCientifico();
        }

        /// <summary>Triggers the Sculptor rescue distress call sequence.</summary>
        public void TriggerSculptorCall()
        {
            if (Manager != null) Manager.OnAuxilioEscultor();
        }

        /// <summary>Triggers the Tailor rescue sequence.</summary>
        public void TriggerTailorCall()
        {
            if (Manager != null) Manager.OnAuxilioModista();
        }

        /// <summary>Triggers the Fortune Teller rescue distress call sequence.</summary>
        public void TriggerFortuneTellerCall()
        {
            if (Manager != null) Manager.OnAuxilioTarotista();
        }

        /// <summary>Triggers the vendor rescued sequence and unlocks standard vendor.</summary>
        public void TriggerVendorRescue()
        {
            if (Manager != null) Manager.OnVendedorRescatado();
        }

        /// <summary>Displays boss defeat notification banner and text.</summary>
        public void TriggerBossDefeated(int bossTypeId, string bossNameLocalizationId, float textSpeedMultiplier = 1f)
        {
            if (Manager != null)
                Manager.OnBossDefeated(bossTypeId, bossNameLocalizationId, textSpeedMultiplier);
        }

        /// <summary>Hides active boss defeat message banner.</summary>
        public void HideBossDefeatedMessage()
        {
            if (Manager != null) Manager.HideBossDefeatedMessage();
        }

        /// <summary>Refreshes minimap icon markers for all NPC locations.</summary>
        public void RefreshMinimapMarkers()
        {
            if (Manager != null) Manager.ActualizarMarcasMapa();
        }

        /// <summary>Resets all NPC story flags and appearance states.</summary>
        public void ResetStoryState()
        {
            if (Manager != null) Manager.ResetearEstado();
        }
    }
}
