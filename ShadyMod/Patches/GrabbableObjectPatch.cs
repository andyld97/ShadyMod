using GameNetcodeStuff;
using HarmonyLib;
using ShadyMod.Interactions;
using ShadyMod.Model;
using ShadyMod.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class GrabbableObjectPatch
    {
        public static readonly Dictionary<GrabbableObject, PlayerBoxInfo> PlayerBoxes = [];

        private const int MAX_PLAYER_IN_BOX = 2;

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ItemActivate))]
        public static void OnItemActivate(GrabbableObject __instance, bool used, bool buttonDown = true)
        {
            if (!AssetInfo.IsShadyItem(__instance.name))
                return;

            ShadyMod.Logger.LogDebug($"OnItemActivate called: {__instance.name}!");

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

            if (PlayerBoxes.ContainsKey(__instance))
            {
                foreach (var player in PlayerBoxes[__instance].Players.ToList())
                {
                    // Player drops all items to ensure that the bug won't exists, otherwise the player will be small even if it drops the head.
                    // Maybe I can fix that later, but this is also a funny workaround too!
                    RemovePlayerFromBox(__instance, player, true);
                    player.DropAllHeldItems(true, false);
                }
            }

            var item = AssetInfo.GetShadyNameByName(__instance.name);
            if (item != null && item.ItemType == ItemType.McHead)
            {
                ShadyMod.Logger.LogDebug($"Item found: {item.Name}");

                if (item.Name == "head-paul")
                {
                    __instance.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    __instance.itemProperties.positionOffset = new Vector3(0f, 0.322f, -0.2f);

                    // Check if player is mounted anywhere in any playerbox
                    foreach (var box in PlayerBoxes)
                    {
                        var player = __instance.playerHeldBy;
                        if (box.Value.Players.Contains(player))
                        {
                            ShadyMod.Logger.LogDebug($"Player removed due to head dropped!");

                            RemovePlayerFromBox(box.Key, player, true);
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
                if (!PlayerBoxes.ContainsKey(__instance))
                    PlayerBoxes.Add(__instance, new PlayerBoxInfo());
                else if (PlayerBoxes[__instance].Players.Contains(__instance.playerHeldBy))
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

                    // Check if player is mounted anywhere in any playerbox
                    foreach (var box in PlayerBoxes)
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

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
        public static void OnUpdate(GrabbableObject __instance)
        {
            var asset = AssetInfo.GetShadyNameByName(__instance.name);

            if (asset == null || asset.ItemType != ItemType.PlayerBox)
                return;

            var now = DateTime.Now;

            if (!PlayerBoxes.ContainsKey(__instance))
                PlayerBoxes.Add(__instance, new PlayerBoxInfo());

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                if (PlayerBoxes[__instance].ResetTime > DateTime.MinValue && (now - PlayerBoxes[__instance].ResetTime).TotalSeconds < 2)
                {
                    // Debugging: 
                    // ShadyMod.Logger.LogDebug("Skipping player add due to recent reset!");
                }
                else
                {
                    PlayerBoxes[__instance].ResetTime = DateTime.MinValue;

                    if (!PlayerBoxes[__instance].Discard)
                    {
                        if (__instance.playerHeldBy == null && PlayerBoxes[__instance].Players.Count < MAX_PLAYER_IN_BOX)
                        {
                            for (int i = 0; i < StartOfRound.Instance.allPlayerObjects.Length; i++)
                            {
                                // Find all nearby players
                                var player = StartOfRound.Instance.allPlayerScripts[i];

                                if (Vector3.Distance(player.transform.position, __instance.transform.position) <= 1f && player.IsSmall())
                                {
                                    if (!PlayerBoxes[__instance].Players.Contains(player))
                                        AddPlayerToBox(__instance, player);
                                }

                                if (PlayerBoxes[__instance].Players.Count >= MAX_PLAYER_IN_BOX)
                                    break;
                            }
                        }
                    }
                    else
                        ShadyMod.Logger.LogDebug("Skipping player add due to discard set!");
                }
            }

            float posOffset = 0f;
            List<PlayerControllerB> toRemove = [];
            foreach (var player in PlayerBoxes[__instance].Players)
            {
                if (!player.IsSmall())
                {
                    toRemove.Add(player);
                    continue;
                }

                // Update player position
                player.transform.position = __instance.transform.position + new Vector3(posOffset, 0.0f, posOffset);
                posOffset += .15f;
            }

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                foreach (var player in toRemove)
                    RemovePlayerFromBox(__instance, player);
            }
        }

        public static void AddPlayerToBox(GrabbableObject __instance, PlayerControllerB player)
        {
            ShadyMod.Logger.LogDebug($"Adding player {player.name} to box!");

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.grabSFX, 1f);
            
            PerkNetworkHandler.Instance.AddPlayerToBoxServerRpc(__instance.NetworkObjectId, (int)player.playerClientId);

            player.playerCollider.enabled = false;
        }

        public static void RemovePlayerFromBox(GrabbableObject __instance, PlayerControllerB player, bool disablePerks = false)
        {
            ShadyMod.Logger.LogDebug($"Removing player {player.name} from box!");

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.dropSFX, 1f);

            PerkNetworkHandler.Instance.RemovePlayerFromBoxServerRpc(__instance.NetworkObjectId, (int)player.playerClientId, disablePerks);         

            player.playerCollider.enabled = true;
        }
    }
}