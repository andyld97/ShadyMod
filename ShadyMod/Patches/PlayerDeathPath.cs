using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class PlayerDeathPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        public static void OnPlayerDeath(PlayerControllerB __instance, Vector3 bodyVelocity, bool spawnBody = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, Vector3 positionOffset = default(Vector3))
        {
            ShadyMod.Logger.LogDebug($"#### Player {__instance.playerUsername} has died! Cause of death: {causeOfDeath}");
            ShadyMod.DisablePerks(__instance);
        }
    }
}
