using GameNetcodeStuff;

namespace ShadyMod.Perks
{
    public class EnemyStunnPerk : PerkBase
    {
        public override string Name => "Enemy Stunn Perk";

        public override string Description => "Stunns all nearby enemies for 30 seconds";

        public override string TriggerItemName => "jedon";

        public override bool CanPerkBeIncreased => true;

        public override void OnUpdate(PlayerControllerB player)
        {
            base.OnUpdate(player);

            if (StartOfRound.Instance.inShipPhase)
                return;

            var enemys = Helper.GetNearbyEnemys(player.transform.position, 5);
            if (enemys.Count > 0)
            {
                bool found = false;
                foreach (var enemy in enemys)
                {
                    if (enemy.stunnedByPlayer != null && enemy.stunnedByPlayer.playerUsername == player.playerUsername)
                        continue;

                    ShadyMod.Logger.LogDebug($"#### Stunning nearby enemy {enemy.name} ...");
                    if (enemy.dieSFX != null)
                        player.movementAudio.PlayOneShot(enemy.dieSFX);

                    int seconds = 10;
                    if (ShouldIncreasePerk(player))
                        seconds = 30;

                    enemy.SetEnemyStunned(true, seconds, player);
                    found = true;
                }

                if (found)
                {
                    Helper.DisplayTooltip("You scared the enemys, time to leave (be fast)!");
                    player.DestroyItemInSlotAndSync(player.currentItemSlot);
                }
            }
        }
    }
}
