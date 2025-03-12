using GameNetcodeStuff;
using HarmonyLib;
using Mono.Cecil.Cil;
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
        private static readonly Dictionary<GrabbableObject, PlayerControllerB> itemToPlayerMap = [];
        public static readonly Dictionary<GrabbableObject, PlayerBoxInfo> playersInBox = [];

        private const int MAX_PLAYER_IN_BOX = 2;

        //[HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.InteractItem))]
        //public static void OnInteract()
        //{
        //    ShadyMod.Logger.LogDebug("#### OnInteract called!");
        //}

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

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItem))]
        public static void OnDiscardItem(GrabbableObject __instance)
        {
            if (itemToPlayerMap.TryGetValue(__instance, out var player))
            {
                ShadyMod.DisablePerks(player);
                itemToPlayerMap.Remove(__instance);
            }

            // TODO:
            // Problem 1: Fall behandeln, wenn Spieler-Kopf wegwirft oder sein Perk deaktiviert wird, dann soll er aus der PlayerBox entfernt werden,
            // sofern er drin ist.
            // Problem 2: Beim Discard klappt es manchmal, dass die Spieler rausgeschmissen werden, aber nicht immer!

            if (playersInBox.ContainsKey(__instance))
            {
                foreach (var playerBoxed in playersInBox[__instance].Players.ToList()) // ToList() important (due to removing elements from list)!   
                    RemovePlayerFromBox(__instance, playerBoxed);

                playersInBox[__instance].Players.Clear();
                playersInBox[__instance].PlayerHeldBy = null;
            }
            else
            {
                ShadyMod.Logger.LogDebug("### PlayerBox not found in playersInBox!");
            }

            var item = AssetInfo.GetShadyNameByName(__instance.name);
            if (item != null && item.ItemType == ItemType.McHead)
            {
                if (item.Name == "head-paul")
                {
                    __instance.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
                    __instance.itemProperties.positionOffset = new Vector3(0f, 0.322f, -0.2f);
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem))]
        public static void OnGrabItem(GrabbableObject __instance)
        {
            if (!AssetInfo.IsShadyItem(__instance.name))
                return;

            itemToPlayerMap[__instance] = __instance.playerHeldBy;          
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
                for (int i = 0; i < StartOfRound.Instance.allPlayerObjects.Length; i++)
                {
                    // Find all nearby players
                    var player = StartOfRound.Instance.allPlayerScripts[i];

                    if (Vector3.Distance(player.transform.position, __instance.transform.position) <= 1f && player.IsSmall())
                    {
                        if (!playersInBox[__instance].Players.Contains(player))
                            AddPlayerToBox(__instance, player);
                    }

                    if (playersInBox[__instance].Players.Count >= MAX_PLAYER_IN_BOX)
                        break;
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

        private static void AddPlayerToBox(GrabbableObject __instance, PlayerControllerB player)
        {
            __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.grabSFX, 1f);
            playersInBox[__instance].Players.Add(player);   
            player.playerCollider.enabled = false;  
        }   

        private static void RemovePlayerFromBox(GrabbableObject __instance, PlayerControllerB player)
        {   
            __instance.gameObject.GetComponent<AudioSource>().PlayOneShot(__instance.itemProperties.dropSFX, 1f);
            playersInBox[__instance].Players.Remove(player);

            if (playersInBox[__instance].PlayerHeldBy != null)
                player.transform.position = playersInBox[__instance].PlayerHeldBy!.transform.position + new Vector3(1.25f, 0f, 1.25f);

            player.playerCollider.enabled = true;   
        }
    }
}