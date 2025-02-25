using GameNetcodeStuff;

namespace ShadyMod.Perks
{
    public class EnemyStunnPerk : PerkBase
    {
        public override string Name => "Enemy Stunn Perk";

        public override string Description => "Stunns all nearby enemies for 30 seconds";

        public override string TriggerItemName => "jedon";

        public override void OnUpdate(PlayerControllerB player)
        {
            base.OnUpdate(player);

            if (StartOfRound.Instance.inShipPhase)
                return;

            var enemys = Helper.GetNearbyEnemys(player.transform.position);
            if (enemys.Count > 0)
            {
                bool found = false;
                foreach (var enemy in enemys)
                {
                    if (enemy.stunnedByPlayer != null && enemy.stunnedByPlayer.playerUsername == player.playerUsername)
                        continue;

                    ShadyMod.Logger.LogDebug($"#### Stunning nearby enemy {enemy.name} ...");
                    player.movementAudio.PlayOneShot(enemy.dieSFX);
                    enemy.SetEnemyStunned(true, 30, player);
                    found = true;
                }

                if (found)
                    player.DestroyItemInSlotAndSync(player.currentItemSlot);
            }
        }
    }
}
