using System;
using Unity.Netcode;
using UnityEngine;

namespace ShadyMod.Network
{
    public class PerkNetworkHandler : NetworkBehaviour
    {
        public static PerkNetworkHandler Instance { get; private set; } = null!;

        public static event Action<string>? MessageEvent;

        public override void OnNetworkSpawn()
        {
            MessageEvent = null;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject?.GetComponent<NetworkObject>()?.Despawn(); // hier gibt es zwischendurch eine Exception: temp mit ? bei gameObject Und GetComponent!
            Instance = this;

            base.OnNetworkSpawn();
        }

        // Hint: Server is also a CLIENT, and also recieves ClientRPC messages!

        [ClientRpc]
        public void EventClientRpc(string eventName)
        {
            MessageEvent?.Invoke(eventName);    
        }

        [ServerRpc(RequireOwnership = false)]
        public void EventServerRpc(string eventName)
        {
            EventClientRpc(eventName);
        }

        [ServerRpc(RequireOwnership = false)]
        public void TeleportPlayerOutServerRpc(int playerObj, Vector3 teleportPos)
        {
            TeleportPlayerOutClientRpc(playerObj, teleportPos);
        }

        [ClientRpc]
        public void TeleportPlayerOutClientRpc(int playerObj, Vector3 teleportPos)
        {
            // if (!StartOfRound.Instance.allPlayerScripts[playerObj].IsOwner)
            {
                var player = StartOfRound.Instance.allPlayerScripts[playerObj];
                player.TeleportPlayer(teleportPos);
            }
        }
    }
}