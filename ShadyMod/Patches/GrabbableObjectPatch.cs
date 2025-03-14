using GameNetcodeStuff;
using HarmonyLib;
using ShadyMod.Interactions;
using ShadyMod.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class GrabbableObjectPatch
    {
        public static readonly Dictionary<GrabbableObject, PlayerBoxInfo> playersInBox = [];

        private const int MAX_PLAYER_IN_BOX = 2;

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemActivate))]
        public static void OnItemActivate(GrabbableObject __instance, bool used, bool buttonDown = true)
        {
            if (!AssetInfo.IsShadyItem(__instance.name))
                return;

            ShadyMod.Logger.LogDebug($"#### OnItemActivate called: {__instance.name}!");

            var info = AssetInfo.GetShadyNameByName(__instance.name);
            if (info == null)
                return;

            switch (info.ItemType)
            {
                case ItemType.McHead:
                    {
                        if (StartOfRound.Instance.inShipPhase)
                            return;

                        PlayerTeleportInteraction.Execute(__instance, __instance.playerHeldBy, info);
                    }
                    break;
                case ItemType.Donut:
                case ItemType.BadDonut:
                    {
                        if (StartOfRound.Instance.inShipPhase)
                            return;

                        DonutInteraction.ExecuteDonutInteraction(__instance, __instance.playerHeldBy, info);
                    }
                    break;
                case ItemType.Robot:
                    {
                        __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.grabSFX, 1f);
                    }
                    break;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItem))]
        public static void OnDiscardItem(GrabbableObject __instance)
        {
            ShadyMod.DisablePerks(__instance.playerHeldBy);

            if (playersInBox.ContainsKey(__instance))
            {
                foreach (var player in playersInBox[__instance].Players.ToList())
                    RemovePlayerFromBox(__instance, player);
            }
            else
                ShadyMod.Logger.LogDebug("### PlayerBox not found in playersInBox!");

            var item = AssetInfo.GetShadyNameByName(__instance.name);
            if (item != null && item.ItemType == ItemType.McHead)
            {
                if (item.Name == "head-paul")
                {
                    __instance.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    __instance.itemProperties.positionOffset = new Vector3(0f, 0.322f, -0.2f);

                    // Check if player is mounted anywhere in any playerbox
                    foreach (var box in playersInBox)
                    {
                        if (box.Value.Players.Contains(__instance.playerHeldBy))
                        {
                            RemovePlayerFromBox(box.Key, __instance.playerHeldBy);
                            break;
                        }
                    }
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem))]
        public static void OnGrabItem(GrabbableObject __instance)
        {
            if (!AssetInfo.IsShadyItem(__instance.name))
                return;
        
            ShadyMod.EnablePerk(__instance, __instance.playerHeldBy);

            var item = AssetInfo.GetShadyNameByName(__instance.name);   
            if (item.ItemType == ItemType.PlayerBox)
            {
                if (playersInBox.ContainsKey(__instance))
                    playersInBox[__instance].PlayerHeldBy = __instance.playerHeldBy;
                else 
                    playersInBox.Add(__instance, new PlayerBoxInfo() { PlayerHeldBy = __instance.playerHeldBy });

                if (playersInBox.ContainsKey(__instance) && playersInBox[__instance].Players.Contains(__instance.playerHeldBy))
                {
                    // Ensure player will be removed if it is already in the box
                     RemovePlayerFromBox(__instance, __instance.playerHeldBy);
                }
            }
            else if (item.ItemType == ItemType.McHead)
            {
                if (item.Name == "head-paul")
                {
                    const float scaleFactor = 0.5f;
                    __instance.transform.localScale = new Vector3(__instance.transform.localScale.x * scaleFactor, __instance.transform.localScale.y * scaleFactor, __instance.transform.localScale.z * scaleFactor);
                    __instance.itemProperties.positionOffset = new Vector3(0f, 0.15f, -0.05f);
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
        public static void OnUpdate(GrabbableObject __instance)
        {
            var asset = AssetInfo.GetShadyNameByName(__instance.name);

            if (asset == null || asset.ItemType != ItemType.PlayerBox)
                return;

            if (!playersInBox.ContainsKey(__instance))
                playersInBox.Add(__instance, new PlayerBoxInfo() { PlayerHeldBy = __instance.playerHeldBy });

            if (__instance.playerHeldBy == null && playersInBox[__instance].Players.Count < MAX_PLAYER_IN_BOX)
            {
                float posOffset1 = 0f;
                for (int i = 0; i < StartOfRound.Instance.allPlayerObjects.Length; i++)
                {
                    // Find all nearby players
                    var player = StartOfRound.Instance.allPlayerScripts[i];

                    if (Vector3.Distance(player.transform.position, __instance.transform.position) <= 1f && player.IsSmall())
                    {
                        if (!playersInBox[__instance].Players.Contains(player))
                            AddPlayerToBox(__instance, player, Vector3.zero);
                    }

                    if (playersInBox[__instance].Players.Count >= MAX_PLAYER_IN_BOX)
                        break;

                    posOffset1 += .1f;
                }
            }

            float posOffset = 0f;
            List<PlayerControllerB> toRemove = [];
            foreach (var player in playersInBox[__instance].Players)
            {
                if (!player.IsSmall())
                {
                    toRemove.Add(player);
                    continue;
                }

                // Update player position
                player.transform.position = __instance.transform.position + new Vector3(posOffset, 0.0f, posOffset);
                posOffset += .1f;
            }

            foreach (var player in toRemove)
                RemovePlayerFromBox(__instance, player);
        }

        // Hint: I tried do this with netcode, but it was not working as expected!
        // This way each client handles the player box logic on its own, and I can live with the trade of that sometimes if the player
        // will drop the box the mounted players won't be removed. But either the player can try it again, or the player can drop the head
        // or switch the slot to be dismounted or grab the box, that should also free the players!
        // But this way it looks way smoother than using netcode!

        public static void AddPlayerToBox(GrabbableObject __instance, PlayerControllerB player, Vector3 pos)
        {
            ShadyMod.Logger.LogWarning($"#### Adding player {player.name} to box!");

            __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.grabSFX, 1f);
            playersInBox[__instance].Players.Add(player);
            player.playerCollider.enabled = false;  
        }

        public static void RemovePlayerFromBox(GrabbableObject __instance, PlayerControllerB player)
        {
            ShadyMod.Logger.LogWarning($"#### Removing player {player.name} from box!");

            __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.dropSFX, 1f);
            playersInBox[__instance].Players.Remove(player);

            if (playersInBox[__instance].PlayerHeldBy != null)
                player.transform.position = playersInBox[__instance].PlayerHeldBy!.transform.position + new Vector3(1.25f, 0f, 1.25f);

            player.playerCollider.enabled = true;   
        }
    }
}