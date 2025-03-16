using ShadyMod.Model;
using ShadyMod.Patches;
using System;
using System.Linq;
using System.Threading.Tasks;
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
            var player = StartOfRound.Instance.allPlayerScripts[playerObj];
            player.TeleportPlayer(teleportPos);
        }

        #region Player Box


        [ServerRpc(RequireOwnership = false)]
        public void AddPlayerToBoxServerRpc(ulong networkObjId, int playerObj)
        {
            AddPlayerToBoxClientRpc(networkObjId, playerObj);
        }

        [ClientRpc]
        public void AddPlayerToBoxClientRpc(ulong networkObjId, int playerObj)
        {
            foreach (var gObj in NetworkManager.SpawnManager.SpawnedObjects)
            {
                if (gObj.Value.NetworkObjectId != networkObjId)
                    continue;

                var grabbableObject = gObj.Value.GetComponent<GrabbableObject>();
                var player = StartOfRound.Instance.allPlayerScripts[playerObj];

                ShadyMod.Logger.LogDebug($"#### Adding player {player.name} to box (RPC)!");

                if (GrabbableObjectPatch.PlayerBoxes.ContainsKey(grabbableObject))
                {
                    if (!GrabbableObjectPatch.PlayerBoxes[grabbableObject].Players.Contains(player))
                        GrabbableObjectPatch.PlayerBoxes[grabbableObject].Players.Add(player);
                }
                else
                    GrabbableObjectPatch.PlayerBoxes.Add(grabbableObject, new PlayerBoxInfo() { Players = [player] });

                player.playerCollider.enabled = false;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RemovePlayerFromBoxServerRpc(ulong networkObjId, int playerObj, bool disablePerks)
        {
            RemovePlayerFromBoxClientRpc(networkObjId, playerObj, disablePerks);
            ResetDiscardClientRpc(networkObjId);           
        }

        [ClientRpc]
        public void ResetDiscardClientRpc(ulong networkObjId)
        {
            var now = DateTime.Now; 
            foreach (var gObj in NetworkManager.SpawnManager.SpawnedObjects)
            {
                if (gObj.Value.NetworkObjectId != networkObjId)
                    continue;

                var grabbableObject = gObj.Value.GetComponent<GrabbableObject>();
                GrabbableObjectPatch.PlayerBoxes[grabbableObject].ResetTime = now;
                GrabbableObjectPatch.PlayerBoxes[grabbableObject].Discard = false;
            }
        }

        [ClientRpc]
        public void RemovePlayerFromBoxClientRpc(ulong networkObjId, int playerObj, bool disablePerks)
        {
            foreach (var gObj in NetworkManager.SpawnManager.SpawnedObjects)
            {
                if (gObj.Value.NetworkObjectId != networkObjId)
                    continue;

                var grabbableObject = gObj.Value.GetComponent<GrabbableObject>();
                var player = StartOfRound.Instance.allPlayerScripts[playerObj];

                ShadyMod.Logger.LogDebug($"#### Removing player {player.name} from box (RPC)!");

                if (GrabbableObjectPatch.PlayerBoxes.ContainsKey(grabbableObject) && GrabbableObjectPatch.PlayerBoxes[grabbableObject].Players.Contains(player))
                {
                    GrabbableObjectPatch.PlayerBoxes[grabbableObject].Discard = true;
                    GrabbableObjectPatch.PlayerBoxes[grabbableObject].Players.Remove(player);

                    player.transform.position = grabbableObject.transform.position + new Vector3(1.25f, 0f, 1.25f);
                    player.playerCollider.enabled = true;

                    if (disablePerks)
                        ShadyMod.DisablePerks(player, true);
                }
            }
        }

        #endregion
    }
}