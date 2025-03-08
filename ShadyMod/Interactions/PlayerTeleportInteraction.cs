using GameNetcodeStuff;
using ShadyMod.Model;
using ShadyMod.Network;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadyMod.Interactions
{
    public static class PlayerTeleportInteraction
    {
        private const float teleportOffset = .5f;

        private readonly static Dictionary<string, string> SteamNameMapping = new Dictionary<string, string>()
        {
            { "belebt", "belebt" },
            { "paul", "vette" },
            { "lasse", "Lasse" },
            { "aveloth", "aveloth" },
            { "andy", "Andy" },
            { "jedon", "JedonFT" },
            { "patrick", "kxmischFxC" }
        };

        //private readonly static Dictionary<string, string> SteamNameMapping = new Dictionary<string, string>()
        //{
        //    { "belebt", "Player #0" },
        //    { "paul", "vette" },
        //    { "lasse", "Lasse" },
        //    { "aveloth", "aveloth" },
        //    { "andy", "Player #1" },
        //    { "jedon", "JedonFT" },
        //    { "patrick", "kxmischFxC" }
        //};

        public static void Execute(GrabbableObject currentItem, PlayerControllerB self, AssetInfo itemInfo)
        {
            string itemSearchName = currentItem.name.ToLower().Replace("(clone)", string.Empty);
            ShadyMod.Logger.LogDebug($"#### Item Search Name: {itemSearchName}");

            if (SteamNameMapping.ContainsKey(itemSearchName))
            {
                string targetPlayerName = SteamNameMapping[itemSearchName];

                // Search for player with the given target name
                bool found = false;

                for (int i = 0; i < StartOfRound.Instance.allPlayerObjects.Length; i++)
                {
                    var player = StartOfRound.Instance.allPlayerScripts[i];

                    if (player.playerUsername.Contains(targetPlayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (player.playerUsername == self.playerUsername)
                            return;

                        ShadyMod.Logger.LogDebug($"#### Player found: {player.playerUsername} ...");

                        if (player.isPlayerDead)
                        {
                            bool killCurrentPlayer = true;

                            if (player.deadBody != null)
                                killCurrentPlayer = Helper.GetRandomBoolean();

                            if (killCurrentPlayer)
                                self.KillPlayer(Vector3.zero, false, CauseOfDeath.Fan);
                            else
                            {
                                Helper.DisplayTooltip($"Lucky you (@{player.playerUsername})! Maybe you can bring back the dead body!");
                                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                                PerkNetworkHandler.Instance.TeleportPlayerOutServerRpc((int)self.playerClientId, new Vector3(player.deadBody.spawnPosition.x + teleportOffset, player.deadBody.spawnPosition.y, player.deadBody.spawnPosition.z + teleportOffset));
                                ShadyMod.DisablePerks(self);
                            }
                        }
                        else
                        {
                            // Teleport to the player
                            ShadyMod.Logger.LogDebug($"#### Teleporting player {player.playerUsername}");

                            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                            PerkNetworkHandler.Instance.TeleportPlayerOutServerRpc((int)self.playerClientId, new Vector3(player.transform.position.x + teleportOffset, player.transform.transform.position.y, player.transform.position.z + teleportOffset));
                            ShadyMod.DisablePerks(self);
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    ShadyMod.Logger.LogWarning($"#### Target Player \"{targetPlayerName}\" not found to teleport to!");
                    Helper.DisplayTooltip($"Target Player \"{targetPlayerName}\" not found to teleport to (@{self.playerUsername})!");
                    ShadyMod.DisablePerks(self);
                    self.DestroyItemInSlotAndSync(self.currentItemSlot);
                    return;
                }
            }
            else
                ShadyMod.Logger.LogWarning("#### Name-Mapping not found!");

            self.DestroyItemInSlotAndSync(self.currentItemSlot);
            ShadyMod.DisablePerks(self);
        }
    }
}