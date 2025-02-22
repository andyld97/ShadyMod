using GameNetcodeStuff;
using HarmonyLib;
using Newtonsoft.Json;
using ShadyMod.Model;
using ShadyMod.Network;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShadyMod.Patches
{
    [HarmonyPatch]
    public class NetworkObjectManager
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Start))]
        public static void Init()
        {
            if (networkPrefab != null)
                return;

            networkPrefab = (GameObject)ShadyMod.assets!.LoadAsset("PerkNetworkHandler");
            networkPrefab.AddComponent<PerkNetworkHandler>();

            NetworkManager.Singleton.AddNetworkPrefab(networkPrefab);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SpawnNetworkHandler()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                var networkHandlerHost = Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        static void SubscribeToHandler()
        {
            PerkNetworkHandler.MessageEvent += ReceivedEventFromServer;
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))] // OnNetworkDespawn (weitere Idee)
        static void UnsubscribeFromHandler()
        {
            PerkNetworkHandler.MessageEvent -= ReceivedEventFromServer;
        }

        static void ReceivedEventFromServer(string eventName)
        {
            Debug.Log($"Recieved event from server: {eventName}");

            NetworkMessage? nm = JsonConvert.DeserializeObject<NetworkMessage>(eventName);
            if (nm != null)
            {
                Debug.Log($"Action to execute is: {nm.Action}");

                foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
                {
                    var playerController = kvp.Value.GetComponent<PlayerControllerB>();
                    if (playerController != null)
                    {
                        if (playerController.playerUsername == nm.PlayerName)
                        {
                            Debug.Log("#### Player found: " + playerController.playerUsername);
                            var perk = ShadyMod.Perks.FirstOrDefault(p => p.Name == nm.PerkName);
                            if (perk == null)
                                return;

                            switch (nm.Action)
                            {
                                case "apply":
                                    perk.Apply(playerController, true);
                                    break;
                                case "reset":
                                    perk.Reset(playerController, true);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public static void SendEventToClients(string eventName)
        {
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                return;

            PerkNetworkHandler.Instance.EventClientRpc(eventName);
        }

        static GameObject networkPrefab = null!;
    }
}