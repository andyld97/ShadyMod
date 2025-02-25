using GameNetcodeStuff;
using System.Linq;

namespace ShadyMod.Perks
{
    public class EnemyKillPerk : PerkBase
    {
        public override string Name => "Enemy Kill";

        public override string Description => "Kills all nearby enemys";

        public override string TriggerItemName => "lasse";

        public override void OnUpdate(PlayerControllerB player)
        {
            base.OnUpdate(player);

            if (StartOfRound.Instance.inShipPhase)
                return;

            var enemys = Helper.GetNearbyEnemys(player.transform.position);
            if (enemys.Count > 0)
            {
                bool enemyFound = false;
                foreach (var enemy in enemys)
                {
                    ShadyMod.Logger.LogDebug($"#### Killing nearby enemy {enemy.name} ...");
                    player.movementAudio.PlayOneShot(enemy.dieSFX);
                    enemy.KillEnemy();
                    enemyFound = true;
                }

                if (enemyFound)
                    player.DestroyItemInSlotAndSync(player.currentItemSlot);
            }
        }
    }
}