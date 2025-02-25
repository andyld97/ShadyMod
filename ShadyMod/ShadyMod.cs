using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using Newtonsoft.Json;
using ShadyMod.Model;
using ShadyMod.Network;
using ShadyMod.Perks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;

namespace ShadyMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID)]
public class ShadyMod : BaseUnityPlugin
{
    public static ShadyMod Instance { get; private set; } = null!;

    internal new static ManualLogSource Logger { get; private set; } = null!;

    internal static Harmony? Harmony { get; set; }

    public static AssetBundle? assets = null;
   
    #region Private Memebers

    private bool isInitalized = false;
    private float defaultPlayerMovementSpeed = 0f;
    private Vector3 defaultPlayerScale = Vector3.zero;
    private Vector3 defaultCameraPos = Vector3.zero;

    private float defaultJumpForce = 0f;
    private bool lastPlayerActionPerformed = false;

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

    #endregion

    #region Initialization 

    private void Awake()
    {
        // Only once
        NetcodePatcher();

        Logger = base.Logger;
        Instance = this;

        Patch();

        assets = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "shady"));
        if (assets == null)
        {
            Logger.LogError("Failed to load custom assets."); 
            return;
        }

        foreach (var assetMeta in AssetInfo.INSTANCE)
        {
            Item asset = assets.LoadAsset<Item>($"Assets/AssetStore/shady/Items/{assetMeta.Name}.asset");

            if (asset != null)
            {
                Logger.LogDebug($"#### Loading asset: {assetMeta.Name} with Rarity: {assetMeta.Rarity} ...");
                asset.canBeGrabbedBeforeGameStart = true;

                if (assetMeta.IsHead)
                {
                    asset.rotationOffset = new Vector3(180, 0, 270);
                    asset.positionOffset = new Vector3(0f, 0.322f, -0.2f);
                }

                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(asset.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(asset, assetMeta.Rarity, assetMeta.Moons);

                Logger.LogInfo($"#### Asset {assetMeta.Name} successfully registered!");
            }
            else
                Logger.LogWarning($"#### Asset {assetMeta.Name} not found!");
        }

        // Assign Events
        On.GameNetcodeStuff.PlayerControllerB.Update += PlayerControllerB_Update;
        On.GameNetcodeStuff.PlayerControllerB.BeginGrabObject += PlayerControllerB_BeginGrabObject;
        On.GameNetcodeStuff.PlayerControllerB.SwitchToItemSlot += PlayerControllerB_SwitchToItemSlot;
        On.GameNetcodeStuff.PlayerControllerB.ActivateItem_performed += PlayerControllerB_ActivateItem_performed;
        On.GameNetcodeStuff.PlayerControllerB.DiscardHeldObject += PlayerControllerB_DiscardHeldObject;

        Logger.LogInfo($"#### {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    #endregion

    #region Player Events

    private void PlayerControllerB_DiscardHeldObject(On.GameNetcodeStuff.PlayerControllerB.orig_DiscardHeldObject orig, PlayerControllerB self, bool placeObject, NetworkObject parentObjectTo, Vector3 placePosition, bool matchRotationOfParent)
    {
        orig(self, placeObject, parentObjectTo, placePosition, matchRotationOfParent);
        DisablePerks(self);
        lastPlayerActionPerformed = false;
    }

    private void PlayerControllerB_SwitchToItemSlot(On.GameNetcodeStuff.PlayerControllerB.orig_SwitchToItemSlot orig, GameNetcodeStuff.PlayerControllerB self, int slot, GrabbableObject fillSlotWithItem)
    {
        orig(self, slot, fillSlotWithItem);
        DisablePerks(self);
        EnablePerk(self.ItemSlots[slot], self);
    }

    private void PlayerControllerB_BeginGrabObject(On.GameNetcodeStuff.PlayerControllerB.orig_BeginGrabObject orig, GameNetcodeStuff.PlayerControllerB self)
    {
        orig(self);

        if (!isInitalized)
            return;

        var item = self.currentlyGrabbingObject;
        if (item != null)
        {
            Logger.LogDebug($"#### Player grabbing an item: {item.name}");
            EnablePerk(self.ItemSlots[self.currentItemSlot], self);
        }
    }  

    private void PlayerControllerB_Update(On.GameNetcodeStuff.PlayerControllerB.orig_Update orig, GameNetcodeStuff.PlayerControllerB self)
    {
        orig(self);

        if (!isInitalized)
        {
            // Get default values for perks
            defaultPlayerMovementSpeed = self.movementSpeed;
            defaultPlayerScale = new Vector3(self.transform.localScale.x, self.transform.localScale.y, self.transform.localScale.z);
            defaultCameraPos = new Vector3(self.gameplayCamera.transform.localPosition.x, self.gameplayCamera.transform.localPosition.y, self.gameplayCamera.transform.localPosition.z);
            defaultJumpForce = self.jumpForce;

            Perks =
            [
                new SprintPerk(),
                new SpeedPerk(defaultPlayerMovementSpeed),
                new ScaleSmallPerk(defaultPlayerScale, defaultJumpForce, defaultCameraPos),
                new ScaleBigPerk(defaultPlayerScale, defaultJumpForce, defaultCameraPos),
                new EnemyKillPerk(),
                new EnemyStunnPerk()
            ];

            isInitalized = true;
            Logger.LogInfo("#### Shady Mod Initialization complemeted!");
        }
        else
        {
            var item = self.ItemSlots[self.currentItemSlot];

            if (item != null)
            {
                foreach (var perk in Perks)
                {
                    if (perk.ShouldApply(self, item))
                        perk.OnUpdate(self);
                }
            }
        }
    }

    private void PlayerControllerB_ActivateItem_performed(On.GameNetcodeStuff.PlayerControllerB.orig_ActivateItem_performed orig, PlayerControllerB self, UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        orig(self, context);

        if (!isInitalized)
            return;

        if (context.action.name == "ActivateItem")
        {
            var currentItem = self.ItemSlots[self.currentItemSlot];
            if (currentItem == null)
            {
                // Unabhängig von lastPlayerActionPerformed ausführen, um sicherzustellen, dass lastPlayerActionPerformed nur auf true gesetzt wird, wenn der richtige Zeitpunkt ist und ich an das Item vom Spieler rankomme!
                Logger.LogDebug("#### No item found to activate!");
                return;
            }

            if (lastPlayerActionPerformed)
            {
                Logger.LogDebug("#### Player action is too fast! Ignoring ...");
                return;
            }

            lastPlayerActionPerformed = true;
            Logger.LogDebug("#### Player action is valid! Processing ...");

            string itemSearchName = currentItem.name.ToLower().Replace("(clone)", string.Empty);
            Logger.LogDebug($"#### Item Search Name: {itemSearchName}");

            if (itemSearchName.Contains("donut"))
            {
                if (StartOfRound.Instance.inShipPhase)
                    return;

                Logger.LogDebug("#### Player executing donut action!");

                AudioClip customClip = StartOfRound.Instance.playerJumpSFX;
                if (customClip != null && self.movementAudio != null)
                    self.movementAudio.PlayOneShot(customClip);

                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                PerkNetworkHandler.Instance.TeleportPlayerOutServerRpc((int)self.playerClientId, new Vector3(self.transform.position.x, self.transform.position.y + UnityEngine.Random.Range(10, 20), self.transform.position.z));
                DisablePerks(self);

                lastPlayerActionPerformed = true;
                self.DestroyItemInSlotAndSync(self.currentItemSlot);
                return;
            }

            // Check if this item is a player head
            if (StartOfRound.Instance.inShipPhase || !AssetInfo.INSTANCE.Any(p => p.Name.ToLower().Contains(itemSearchName) && p.IsHead))
                return;

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
                        if (player.playerUsername == GameNetworkManager.Instance.localPlayerController.playerUsername)
                            continue;

                        Logger.LogDebug($"#### Player found: {player.playerUsername} ...");

                        if (player.isPlayerDead)
                        {
                            bool killCurrentPlayer = true;

                            if (self.deadBody != null)
                                killCurrentPlayer = Helper.GetRandomBoolean();

                            if (killCurrentPlayer)
                                self.KillPlayer(Vector3.zero, false, CauseOfDeath.Fan);
                            else
                            {
                                HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                                PerkNetworkHandler.Instance.TeleportPlayerOutServerRpc((int)self.playerClientId, new Vector3(player.deadBody.spawnPosition.x + teleportOffset, player.deadBody.spawnPosition.y, player.deadBody.spawnPosition.z + teleportOffset));
                                DisablePerks(self);
                                lastPlayerActionPerformed = false;
                            }
                        }
                        else
                        {
                            // Teleport to the player
                            Logger.LogDebug($"#### Teleporting player {player.playerUsername}");

                            HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
                            PerkNetworkHandler.Instance.TeleportPlayerOutServerRpc((int)self.playerClientId, new Vector3(player.transform.position.x + teleportOffset, player.transform.transform.position.y, player.transform.position.z + teleportOffset));
                            DisablePerks(self);
                            lastPlayerActionPerformed = false;
                        }

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Logger.LogWarning($"#### Target Player \"{targetPlayerName}\" not found to teleport to!");
                    DisablePerks(self);
                    self.DestroyItemInSlotAndSync(self.currentItemSlot);
                    lastPlayerActionPerformed = false;
                    return;
                }
            }
            else
                Logger.LogWarning("#### Name-Mapping not found!");

            self.DestroyItemInSlotAndSync(self.currentItemSlot);
            DisablePerks(self);
            lastPlayerActionPerformed = false;
        }
    }

    #endregion

    #region Perks

    public static List<PerkBase> Perks = [];

    private void EnablePerk(GrabbableObject item, PlayerControllerB player)
    {
        if (item == null)
            return;

        Perks.ForEach(p => 
        {
            if (p.ShouldApply(player, item))
            {
                Logger.LogDebug($"[PERK]: Applying perk {p.Name} ...");
                p.Apply(player);
            }
        });      
    }

    private void DisablePerks(PlayerControllerB player, bool force = false)
    {
        Logger.LogDebug("[PERK]: Disabling all perks...");
        Perks.ForEach(p => p.Reset(player, force));
        lastPlayerActionPerformed = false;
    }

    #endregion

    #region Harmony Patches

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
    #endregion

    #region Netcode Patcher
    private static void NetcodePatcher()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length > 0)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }
    #endregion
}