using System;
using UnityEngine;
using TwoDefender.SaveSystem;
using Object = UnityEngine.Object;

namespace RombyLib
{
    /// <summary>
    /// Controls Roguelike mode runs, wave rewards, seeded generation, and run statistics.
    /// </summary>
    public class RoguelikeController
    {
        private RoguelikeManager Manager
        {
            get
            {
                var inst = RoguelikeManager.Instance;
                return Romby.IsValid(inst) ? inst : null;
            }
        }

        /// <summary>Checks whether Roguelike mode manager is active in the current scene.</summary>
        public bool IsActive => Manager != null;

        /// <summary>Checks whether current Roguelike run ended in defeat.</summary>
        public bool IsGameOver => Manager != null && Manager.IsGameOver;

        /// <summary>Checks whether current Roguelike run was won.</summary>
        public bool IsVictory => Manager != null && Manager.IsVictory;

        /// <summary>Checks whether the game is currently running in endless/infinite Roguelike loop.</summary>
        public bool IsInfiniteMode => Manager != null && Manager.IsInfiniteMode;

        /// <summary>Checks whether run goals have been completed.</summary>
        public bool IsRunCompleted => Manager != null && Manager.IsRunCompleted;

        /// <summary>Gets the current round/wave index in the Roguelike cycle.</summary>
        public int CurrentRound => Manager != null ? Manager.GetCurrentRound() : 0;

        /// <summary>Elapsed time in seconds for the current Roguelike run.</summary>
        public float ElapsedRunTime
        {
            get
            {
                if (Manager != null) return Manager.GetRunElapsedTime();
                var save = Game.CurrentData;
                return save?.roguelikeRunElapsedTime ?? 0f;
            }
        }

        /// <summary>Total enemies killed during the current Roguelike run.</summary>
        public int TotalEnemiesKilled
        {
            get
            {
                if (Manager != null) return Manager.GetTotalEnemiesKilled();
                var save = Game.CurrentData;
                return save?.roguelikeTotalEnemiesKilled ?? 0;
            }
        }

        /// <summary>Total currency earned during the current Roguelike run.</summary>
        public int TotalMoneyEarned
        {
            get
            {
                if (Manager != null) return Manager.GetTotalMoneyEarned();
                var save = Game.CurrentData;
                return save?.roguelikeTotalMoneyEarned ?? 0;
            }
        }

        /// <summary>Total experience points collected during the current Roguelike run.</summary>
        public int TotalXPEarned
        {
            get
            {
                if (Manager != null) return Manager.GetTotalXPEarned();
                var save = Game.CurrentData;
                return save?.roguelikeTotalXPEarned ?? 0;
            }
        }

        /// <summary>Current run money multiplier.</summary>
        public int MoneyMultiplier
        {
            get
            {
                if (Manager != null) return Manager.GetMoneyMultiplier();
                var save = Game.CurrentData;
                return save?.roguelikeMoneyMultiplier ?? 1;
            }
            set => SetMoneyMultiplier(value);
        }

        /// <summary>Current run random generation seed string.</summary>
        public string Seed
        {
            get
            {
                var save = Game.CurrentData;
                return save?.partidaSemilla ?? string.Empty;
            }
            set
            {
                var save = Game.CurrentData;
                if (save != null) save.partidaSemilla = value;
            }
        }

        /// <summary>Immediately triggers the wave completion reward offering.</summary>
        public void TriggerInstantWaveReward()
        {
            var mgr = Manager;
            if (mgr != null) mgr.TriggerInstantWaveReward();
        }

        /// <summary>Immediately triggers game over sequence for the Roguelike run.</summary>
        public void TriggerGameOver()
        {
            var mgr = Manager;
            if (mgr != null) mgr.TriggerGameOver();
        }

        /// <summary>Transitions victory state into endless mode continuation.</summary>
        public void EnterInfiniteMode()
        {
            var mgr = Manager;
            if (mgr != null) mgr.OnVictoryInfiniteMode();
        }

        /// <summary>Sets custom run money multiplier and synchronizes save data.</summary>
        public void SetMoneyMultiplier(int multiplier)
        {
            var mgr = Manager;
            if (mgr != null) mgr.RestoreMoneyMultiplier(multiplier);

            var save = Game.CurrentData;
            if (save != null) save.roguelikeMoneyMultiplier = multiplier;
        }

        /// <summary>Restores lifetime statistics and elapsed progress for active Roguelike run.</summary>
        public void RestoreRunStats(float elapsedTime, int enemiesKilled, int moneyEarned, int xpEarned)
        {
            var mgr = Manager;
            if (mgr != null)
            {
                mgr.RestoreRoguelikeRunStats(elapsedTime, enemiesKilled, moneyEarned, xpEarned);
            }

            var save = Game.CurrentData;
            if (save != null)
            {
                save.roguelikeRunElapsedTime = elapsedTime;
                save.roguelikeTotalEnemiesKilled = enemiesKilled;
                save.roguelikeTotalMoneyEarned = moneyEarned;
                save.roguelikeTotalXPEarned = xpEarned;
            }
        }
    }

    /// <summary>
    /// Controls New Game Plus progression, post-game final boss phase, and scaled difficulty multipliers.
    /// </summary>
    public class NewGamePlusController
    {
        private NewGamePlusManager Manager
        {
            get
            {
                var inst = NewGamePlusManager.Instance;
                return Romby.IsValid(inst) ? inst : null;
            }
        }

        /// <summary>Checks whether New Game Plus mode is currently active.</summary>
        public bool IsActive
        {
            get
            {
                if (Manager != null) return Manager.NewGamePlusActivo;
                var save = Game.CurrentData;
                return save?.gameState?.newGamePlusActivo ?? false;
            }
            set
            {
                var save = Game.CurrentData;
                if (save?.gameState != null) save.gameState.newGamePlusActivo = value;
                var mgr = Manager;
                if (mgr != null)
                {
                    mgr.SetEstadoGuardado(IsYkonDefeated, value, MoneyMultiplier);
                }
            }
        }

        /// <summary>Checks whether the final boss encounter phase is currently active.</summary>
        public bool IsFinalPhaseActive => Manager != null && Manager.FaseFinalActiva;

        /// <summary>Checks whether final boss Ykon has been defeated in save or live state.</summary>
        public bool IsYkonDefeated
        {
            get
            {
                if (Manager != null) return Manager.YkonDerrotado;
                var save = Game.CurrentData;
                return save?.gameState?.ykonDerrotado ?? false;
            }
            set
            {
                var save = Game.CurrentData;
                if (save?.gameState != null) save.gameState.ykonDerrotado = value;
                var mgr = Manager;
                if (mgr != null)
                {
                    mgr.SetEstadoGuardado(value, IsActive, MoneyMultiplier);
                }
            }
        }

        /// <summary>Active currency multiplier for New Game Plus mode.</summary>
        public int MoneyMultiplier
        {
            get
            {
                if (Manager != null) return Manager.GetMoneyMultiplier();
                var save = Game.CurrentData;
                return save?.gameState?.ngPlusMoneyMultiplier ?? 1;
            }
            set
            {
                var save = Game.CurrentData;
                if (save?.gameState != null) save.gameState.ngPlusMoneyMultiplier = value;
                var mgr = Manager;
                if (mgr != null)
                {
                    mgr.SetEstadoGuardado(IsYkonDefeated, IsActive, value);
                }
            }
        }

        /// <summary>Triggers the final boss encounter sequence.</summary>
        public void ActivateFinalPhase()
        {
            var mgr = Manager;
            if (mgr != null) mgr.ActivarFaseFinal();
        }

        /// <summary>Updates and synchronizes New Game Plus state across live manager and save files.</summary>
        public void SetState(bool ykonDefeated, bool ngPlusActive, int moneyMultiplier)
        {
            var mgr = Manager;
            if (mgr != null)
            {
                mgr.SetEstadoGuardado(ykonDefeated, ngPlusActive, moneyMultiplier);
            }

            var save = Game.CurrentData;
            if (save?.gameState != null)
            {
                save.gameState.ykonDerrotado = ykonDefeated;
                save.gameState.newGamePlusActivo = ngPlusActive;
                save.gameState.ngPlusMoneyMultiplier = moneyMultiplier;
            }
        }
    }
}
