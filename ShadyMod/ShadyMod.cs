using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ShadyMod.Model;
using ShadyMod.Perks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Netcode;
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

                if (assetMeta.Name != "donout")
                    asset.rotationOffset = new Vector3(180, 0, 270);

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
        On.GameNetcodeStuff.PlayerControllerB.ThrowObjectClientRpc += PlayerControllerB_ThrowObjectClientRpc;
        On.GameNetcodeStuff.PlayerControllerB.ThrowObjectServerRpc += PlayerControllerB_ThrowObjectServerRpc;
        On.GameNetcodeStuff.PlayerControllerB.ActivateItem_performed += PlayerControllerB_ActivateItem_performed;

        Logger.LogInfo($"#### {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    #endregion

    #region Player Events

    private void PlayerControllerB_ThrowObjectServerRpc(On.GameNetcodeStuff.PlayerControllerB.orig_ThrowObjectServerRpc orig, GameNetcodeStuff.PlayerControllerB self, Unity.Netcode.NetworkObjectReference grabbedObject, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, int floorYRot)
    {
        orig(self, grabbedObject, droppedInElevator, droppedInShipRoom, targetFloorPosition, floorYRot);
        // DisablePerks(self); --> Scheint Probleme zu machen, daher erstmal mit Client machen!
    }

    private void PlayerControllerB_ThrowObjectClientRpc(On.GameNetcodeStuff.PlayerControllerB.orig_ThrowObjectClientRpc orig, GameNetcodeStuff.PlayerControllerB self, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, Unity.Netcode.NetworkObjectReference grabbedObject, int floorYRot)
    {
        orig(self, droppedInElevator, droppedInShipRoom, targetFloorPosition, grabbedObject, floorYRot);
        DisablePerks(self);
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
                new ScaleBigPerk(defaultPlayerScale, defaultJumpForce, defaultCameraPos)    
            ];

            isInitalized = true;
            Logger.LogInfo("#### Shady Mod Initalization complemeted!");
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
                Logger.LogDebug("#### No item found to teleport to!");
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

            if (itemSearchName.Contains("donout"))
            {
                // TODO: Spieler rng in die Luft teleporiern (10-20 Y Random) oder stamina wieder auffüllen
                Logger.LogDebug("#### Player executing donout action (TEST)!");

                // TODO: Item zerstören

                lastPlayerActionPerformed = true;               
                return;
            }

            if (SteamNameMapping.ContainsKey(itemSearchName))
            {
                string targetPlayerName = SteamNameMapping[itemSearchName];

                // Search for player with the given target name
                bool found = false;
                foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
                {
                    var playerController = obj.Value.GetComponent<PlayerControllerB>();
                    if (playerController == null)
                        continue;

                    if (playerController.playerUsername.Contains(targetPlayerName, StringComparison.OrdinalIgnoreCase))
                    {
                        Logger.LogDebug($"#### Player found: {playerController.playerUsername} ...");

                        // Teleport to the player
                        Logger.LogDebug($"#### Teleporting player {playerController.playerUsername} [ServerPos: {playerController.serverPlayerPosition.FormatVector3()}\n\n OldPlayerPos: {playerController.oldPlayerPosition.FormatVector3()}]");
                        self.TeleportPlayer(playerController.serverPlayerPosition);

                        // TODO
                        // NotServerException: Only the server can reparent NetworkObjects
                        // Scheinbar darf das Teleportieren der Person nur über den Server ausgeführt werden, sprich NetCode!


                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Logger.LogWarning($"#### Target Player \"{targetPlayerName}\" not found to teleport to!");

                    // TODO: Hier ist die später, dass wenn kein Spieler gefunden wird, oder noch besser, wenn festgestellt wird, dass der Spieler
                    // deadge ist, dass man dann selbst auch getötet wird!
                    // Idee von belebt war noch, dass man evtl. auch zur Leiche teleportiert wird (es gibt ja auch die DeathPosition)

                    return;
                }
            }
            else
                Logger.LogWarning("#### Name-Mapping not found!");

            // TODO: Später soll das Item hier zerstört werden, nach einmaliger Benutzung,
            // aber das wäre erstmal nervig zum Testen, daher erstmal auskommentiert:
            // self.DestroyItemInSlotServerRpc(self.currentItemSlot);
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
                Logger.LogInfo($"[PERK]: Applying perk {p.Name} ...");
                p.Apply(player);
            }
        });      
    }

    private void DisablePerks(PlayerControllerB player)
    {
        Logger.LogInfo("[PERK]: Disabling all perks...");
        Perks.ForEach(p => p.Reset(player));
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