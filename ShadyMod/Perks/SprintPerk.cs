using GameNetcodeStuff;

namespace ShadyMod.Perks
{
    public class SprintPerk : PerkBase
    {
        public override string Name => "Sprint Perk";

        public override string Description => "Endless stamina";

        public override string TriggerItemName => "andy";

        public override void OnUpdate(PlayerControllerB player)
        {
            base.OnUpdate(player);
            player.sprintMeter = 1f;
        }
    }
}
