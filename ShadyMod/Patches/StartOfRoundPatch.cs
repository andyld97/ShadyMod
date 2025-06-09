using HarmonyLib;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class StartOfRoundPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        public static void OnStartGame(StartOfRound __instance)
        {
            ShadyMod.Logger.LogDebug("StartOfRound: StartOfRound.StartGame() called, disabling perks for all players.");

            foreach (var playerController in __instance.allPlayerScripts)
                ShadyMod.DisablePerks(playerController, true);
        }
    }
}