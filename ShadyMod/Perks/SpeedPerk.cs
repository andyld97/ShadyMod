using GameNetcodeStuff;

namespace ShadyMod.Perks
{
    public class SpeedPerk : PerkBase
    {
        private const float playerMovementSpeedPerk = 10f;
        private const float movementIncrease = 5f;
        private readonly float defaultMovementSpeed = 0f;

        public SpeedPerk(float defaultMovementSpeed)
        {
            this.defaultMovementSpeed = defaultMovementSpeed;
        }

        public override string Name => "Speed Perk";

        public override string Description => "The player moves faster than normal";

        public override string TriggerItemName => "belebt";

        public override void Apply(PlayerControllerB player, bool force = false)
        {
            if (!force)
            {
                if (isApplied)
                    return;
            }

            base.Apply(player);

            player.movementSpeed = playerMovementSpeedPerk;

            if (ShouldIncreasePerk(player)) 
                player.movementSpeed += movementIncrease;

            if (!force)
                isApplied = true;
        }

        public override void Reset(PlayerControllerB player, bool force = false)
        {
            if (!force)
            {
                if (!isApplied)
                    return;
            }

            base.Reset(player);

            player.movementSpeed = defaultMovementSpeed;

            if (!force)
                isApplied = false;
        }
    }
}
