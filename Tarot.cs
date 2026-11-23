using System;
using System.Collections.Generic;
using UnityEngine;
using TwoDefender.SaveSystem;
using Il2CppCollections = Il2CppSystem.Collections.Generic;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Controls Tarot card drawing, active temporary powerups, discovery index, and power multipliers.
    /// </summary>
    public class TarotController
    {
        private TarotCardManager _cachedCardManager;
        private TarotEffectManager _cachedEffectManager;

        private TarotCardManager CardManager
        {
            get
            {
                if (Romby.IsValid(_cachedCardManager)) return _cachedCardManager;
                _cachedCardManager = TarotCardManager.FindInstance();
                return Romby.IsValid(_cachedCardManager) ? _cachedCardManager : null;
            }
        }

        private TarotEffectManager EffectManager
        {
            get
            {
                if (Romby.IsValid(_cachedEffectManager)) return _cachedEffectManager;
                _cachedEffectManager = Object.FindObjectOfType<TarotEffectManager>();
                return Romby.IsValid(_cachedEffectManager) ? _cachedEffectManager : null;
            }
        }

        /// <summary>Gets whether the Tarot card selection UI is currently active on screen.</summary>
        public bool IsSelectionActive
        {
            get
            {
                var mgr = CardManager;
                return Romby.IsValid(mgr) && Romby.IsValid(mgr.canvasAnimator) && mgr.canvasAnimator.gameObject.activeInHierarchy;
            }
        }

        /// <summary>Gets a snapshot list of currently active Tarot cards with remaining duration.</summary>
        public IReadOnlyList<ActiveTarotCardData> ActiveCards
        {
            get
            {
                var save = Game.CurrentData;
                if (save?.gameState?.activeTarotCards != null)
                {
                    var list = new List<ActiveTarotCardData>();
                    foreach (var card in save.gameState.activeTarotCards)
                    {
                        if (card != null) list.Add(card);
                    }
                    return list;
                }
                return Array.Empty<ActiveTarotCardData>();
            }
        }

        /// <summary>Gets the list of discovered Tarot card IDs stored in save state.</summary>
        public IReadOnlyList<int> DiscoveredCardIndices
        {
            get
            {
                var mgr = CardManager;
                if (Romby.IsValid(mgr))
                {
                    var nativeList = mgr.GetDiscoveredCardIndices();
                    if (nativeList != null)
                    {
                        var result = new List<int>(nativeList.Count);
                        foreach (var idx in nativeList) result.Add(idx);
                        return result;
                    }
                }

                var save = Game.CurrentData;
                if (save?.gameState?.cartasTarotDescubiertas != null)
                {
                    var result = new List<int>();
                    foreach (var idx in save.gameState.cartasTarotDescubiertas) result.Add(idx);
                    return result;
                }
                return Array.Empty<int>();
            }
        }

        /// <summary>Total number of Tarot cards unlocked and discovered by the player.</summary>
        public int DiscoveredCount => DiscoveredCardIndices.Count;

        /// <summary>Triggers Tarot card offering panel for the player.</summary>
        public void Activate(bool requireSkillUnlock = false)
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr))
            {
                mgr.ActivateTarotCards(requireSkillUnlock);
            }
        }

        /// <summary>Cancels and closes any pending Tarot card offering.</summary>
        public void CancelPendingActivation()
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr))
            {
                mgr.CancelPendingActivation();
            }
        }

        /// <summary>Checks whether a specific Tarot power type is currently active.</summary>
        public bool HasActivePower(TarotCardManager.TarotPowerType powerType)
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr)) return mgr.HasActivePower(powerType);

            var effects = EffectManager;
            return Romby.IsValid(effects) && effects.HasActiveEffect(powerType);
        }

        /// <summary>Gets the effective stat multiplier applied by an active Tarot card.</summary>
        public float GetPowerMultiplier(TarotCardManager.TarotPowerType powerType)
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr)) return mgr.GetPowerMultiplier(powerType);

            var effects = EffectManager;
            return Romby.IsValid(effects) ? effects.GetEffectMultiplier(powerType) : 1f;
        }

        /// <summary>Directly applies a Tarot effect with custom multiplier duration.</summary>
        public void ApplyEffect(TarotCardManager.TarotPowerType effectType, float multiplier = 2f)
        {
            var effects = EffectManager;
            if (Romby.IsValid(effects))
            {
                effects.ApplyEffect(effectType, multiplier);
            }
        }

        /// <summary>Removes an active Tarot effect by power type.</summary>
        public void RemoveEffect(TarotCardManager.TarotPowerType effectType)
        {
            var effects = EffectManager;
            if (Romby.IsValid(effects))
            {
                effects.RemoveEffect(effectType);
            }
        }

        /// <summary>Clears all currently active Tarot effects and powerups.</summary>
        public void ClearAllEffects()
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr)) mgr.ClearAllEffects();

            var effects = EffectManager;
            if (Romby.IsValid(effects)) effects.ClearAllEffects();
        }

        /// <summary>Checks if a Tarot card index has been discovered and recorded in the collection.</summary>
        public bool IsCardDiscovered(int cardIndex)
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr)) return mgr.IsCardDiscovered(cardIndex);

            var list = DiscoveredCardIndices;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == cardIndex) return true;
            }
            return false;
        }

        /// <summary>Marks a Tarot card as discovered in live state and save data.</summary>
        public bool RegisterDiscoveredCard(int cardIndex)
        {
            var mgr = CardManager;
            if (Romby.IsValid(mgr))
            {
                return mgr.RegisterDiscoveredCard(cardIndex);
            }

            var save = Game.CurrentData;
            if (save?.gameState != null)
            {
                if (save.gameState.cartasTarotDescubiertas == null)
                    save.gameState.cartasTarotDescubiertas = new Il2CppCollections.List<int>();

                if (!save.gameState.cartasTarotDescubiertas.Contains(cardIndex))
                {
                    save.gameState.cartasTarotDescubiertas.Add(cardIndex);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Rerolls offered Tarot cards during cooperative or single-player selection.</summary>
        public bool TryReroll()
        {
            var mgr = CardManager;
            return Romby.IsValid(mgr) && mgr.TryCoopReroll();
        }

        /// <summary>Checks whether a Tarot power is eligible to appear in the current game run.</summary>
        public static bool CanAppearInRun(TarotCardManager.TarotPowerType powerType)
        {
            return TarotCardManager.PuedeAparecerEnEstaPartida(powerType);
        }

        /// <summary>Counts total number of discoverable Tarot cards possible in the current run.</summary>
        public static int CountPossibleCardsInRun()
        {
            return TarotCardManager.ContarCartasPosiblesEnEstaPartida();
        }
    }
}
