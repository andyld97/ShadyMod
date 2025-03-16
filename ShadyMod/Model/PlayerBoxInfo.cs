using GameNetcodeStuff;
using System;
using System.Collections.Generic;

namespace ShadyMod.Model
{
    public class PlayerBoxInfo
    {
        public List<PlayerControllerB> Players { get; set; } = [];

        public bool Discard { get; set; } = false;

        public DateTime ResetTime { get; set; } = DateTime.MinValue;
    }
}