using GameNetcodeStuff;
using HarmonyLib;
using ShadyMod.Model;
using UnityEngine;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class PlayerPatch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
        public static void OnPlayerDeath(PlayerControllerB __instance, Vector3 bodyVelocity, bool spawnBody = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, Vector3 positionOffset = default(Vector3))
        {
            ShadyMod.Logger.LogDebug($"Player {__instance.playerUsername} has died! Cause of death: {causeOfDeath}");
            ShadyMod.DisablePerks(__instance, true);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DropAllHeldItems))]
        public static void OnDropAllHeldItems(PlayerControllerB __instance)
        {
            ShadyMod.DisablePerks(__instance, true);

            var item = __instance.ItemSlots[__instance.currentItemSlot];

            if (__instance.IsSmall())
            {
                // Reset item scales
                if (item != null)
                {
                    item.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    item.itemProperties.positionOffset = new Vector3(0f, 0.322f, -0.2f);
                }
            }

            if (item != null)
            {
                // Also reset player assoc in PlayersInBox
                var shadyItem = AssetInfo.GetShadyNameByName(item.name);

                if (shadyItem != null && shadyItem.ItemType == ItemType.PlayerBox)
                {
                    foreach (var box in GrabbableObjectPatch.PlayerBoxes)
                    {
                        if (box.Key == item)
                        {
                            foreach (var player in box.Value.Players)
                                GrabbableObjectPatch.RemovePlayerFromBox(box.Key, player, true);
                        }
                    }
                }
            }
        }
    }
}