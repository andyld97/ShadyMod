using Newtonsoft.Json;
using UnityEngine;

namespace ShadyMod.Model
{
    public class NetworkMessage
    {
        public ulong PlayerId { get; set; } = 0;

        public string PlayerName { get; set; } = string.Empty!;

        public string Action { get; set; } = string.Empty!;

        public string PerkName { get; set; } = string.Empty!;   

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
