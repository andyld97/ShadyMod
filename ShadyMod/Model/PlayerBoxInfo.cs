using GameNetcodeStuff;
using System.Collections.Generic;

namespace ShadyMod.Model
{
    public class PlayerBoxInfo
    {
        public List<PlayerControllerB> Players { get; set; } = [];

        public PlayerControllerB? PlayerHeldBy { get; set; } = null!;
    }
}