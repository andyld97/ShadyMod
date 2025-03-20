using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ShadyMod.Model;
using ShadyMod.Perks;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using static LethalLib.Modules.ContentLoader;

namespace ShadyMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(LethalLib.Plugin.ModGUID)]
public class ShadyMod : BaseUnityPlugin
{
    #region Static Memeber
    public static ShadyMod Instance { get; private set; } = null!;

    internal new static ManualLogSource Logger { get; private set; } = null!;

    internal static Harmony? Harmony { get; set; }

    public static AssetBundle? assets = null;
    #endregion

    #region Private Memeber

    private bool isInitalized = false;
    private float defaultPlayerMovementSpeed = 0f;
    private Vector3 defaultPlayerScale = Vector3.zero;
    private Vector3 defaultCameraPos = Vector3.zero;

    private float defaultJumpForce = 0f;

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
                Logger.LogDebug($"Loading asset: {assetMeta.Name} with Rarity: {assetMeta.Rarity} ...");
                asset.canBeGrabbedBeforeGameStart = true;

                if (assetMeta.ItemType == ItemType.McHead)
                {
                    asset.rotationOffset = new Vector3(180, 0, 270);
                    asset.positionOffset = new Vector3(0f, 0.322f, -0.2f);
                }
                else if (assetMeta.ItemType == ItemType.Donut || assetMeta.ItemType == ItemType.BadDonut)
                    asset.positionOffset = new Vector3(0f, 0.15f, -0.1f);
                else if (assetMeta.ItemType == ItemType.Weight)
                    asset.positionOffset = new Vector3(0f, 0.15f, -0.1f);
                else if (assetMeta.ItemType == ItemType.Robot)
                {
                    asset.rotationOffset = new Vector3(0f, 180f, 90f);
                    asset.positionOffset = new Vector3(0.4f, 0.3f, -0.1f);
                }
                else if (assetMeta.ItemType == ItemType.PlayerBox)
                {
                    asset.rotationOffset = new Vector3(180, 0, 270);
                    asset.positionOffset = new Vector3(0f, 0.5f, -0.2f);
                }

                LethalLib.Modules.Utilities.FixMixerGroups(asset.spawnPrefab);
                LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(asset.spawnPrefab);
                LethalLib.Modules.Items.RegisterScrap(asset, assetMeta.Rarity, assetMeta.Moons);     

                if (assetMeta.ItemType == ItemType.PlayerBox)
                {
                    TerminalNode iTerminalNode = assets.LoadAsset<TerminalNode>("Assets/AssetStore/shady/Items/iTerminalNode.asset");
                    LethalLib.Modules.Items.RegisterShopItem(asset, null!, null!, iTerminalNode, 150);
                }

                Logger.LogInfo($"Asset {assetMeta.Name} successfully registered!");
            }
            else
                Logger.LogWarning($"Asset {assetMeta.Name} not found!");
        }

        // Assign Events
        On.GameNetcodeStuff.PlayerControllerB.Update += PlayerControllerB_Update;
        On.GameNetcodeStuff.PlayerControllerB.SwitchToItemSlot += PlayerControllerB_SwitchToItemSlot;
        On.GameNetcodeStuff.PlayerControllerB.DropAllHeldItems += PlayerControllerB_DropAllHeldItems;

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }
    #endregion

    #region Player Events

    private void PlayerControllerB_DropAllHeldItems(On.GameNetcodeStuff.PlayerControllerB.orig_DropAllHeldItems orig, PlayerControllerB self, bool itemsFall, bool disconnecting)
    {
        orig(self, itemsFall, disconnecting);
        DisablePerks(self);
    }

    private void PlayerControllerB_SwitchToItemSlot(On.GameNetcodeStuff.PlayerControllerB.orig_SwitchToItemSlot orig, GameNetcodeStuff.PlayerControllerB self, int slot, GrabbableObject fillSlotWithItem)
    {
        orig(self, slot, fillSlotWithItem);
        DisablePerks(self);
        EnablePerk(self.ItemSlots[slot], self);
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
                new EnemyStunnPerk(),
                new EnemySmallPerk()
            ];

            isInitalized = true;
            Logger.LogInfo("Shady Mod Initialization complemeted!");
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
    #endregion

    #region Perks

    public static List<PerkBase> Perks = [];

    public static void EnablePerk(GrabbableObject item, PlayerControllerB player)
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

    public static void DisablePerks(PlayerControllerB player, bool force = false)
    {
        Logger.LogDebug("[PERK]: Disabling all perks...");
        Perks.ForEach(p => p.Reset(player, force));
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