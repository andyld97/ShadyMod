using GameNetcodeStuff;
using HarmonyLib;
using ShadyMod.Interactions;
using ShadyMod.Model;
using System.Collections.Generic;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class GrabbableObjectPatch
    {
        private static readonly Dictionary<GrabbableObject, PlayerControllerB> itemToPlayerMap = [];

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.InteractItem))]
        public static void OnInteract()
        {
            ShadyMod.Logger.LogDebug("#### OnInteract called!");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemActivate))]
        public static void OnItemActivate(GrabbableObject __instance, bool used, bool buttonDown = true)
        {
            if (!AssetInfo.IsShadyItem(__instance.name))
                return;

            ShadyMod.Logger.LogDebug($"#### OnItemActivate called: {__instance.name}!");

            var info = AssetInfo.GetShadyNameByName(__instance.name);
            if (info == null)
                return;

            if (StartOfRound.Instance.inShipPhase)
                return;

            switch (info.ItemType)
            {
                case ItemType.McHead:
                    PlayerTeleportInteraction.Execute(__instance, __instance.playerHeldBy, info);
                    break;
                case ItemType.Donut:
                case ItemType.BadDonut:
                    DonutInteraction.ExecuteDonutInteraction(__instance, __instance.playerHeldBy, info);
                    break;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItem))]
        public static void OnDiscardItem(GrabbableObject __instance)
        {
            if (itemToPlayerMap.TryGetValue(__instance, out var player))
            {
                ShadyMod.DisablePerks(player);
                itemToPlayerMap.Remove(__instance);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem))]
        public static void OnGrabItem(GrabbableObject __instance)
        {
            if (!AssetInfo.IsShadyItem(__instance.name))
                return;

            itemToPlayerMap[__instance] = __instance.playerHeldBy;          
            ShadyMod.EnablePerk(__instance, __instance.playerHeldBy);
        }
    }
}