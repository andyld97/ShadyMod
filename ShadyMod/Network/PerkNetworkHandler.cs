using System;
using Unity.Netcode;

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

        [ClientRpc]
        public void EventClientRpc(string eventName)
        {
            MessageEvent?.Invoke(eventName);    
        }

        [ServerRpc(RequireOwnership = false)]
        public void EventServerRpc(string eventName)
        {
            
        }
    }
}