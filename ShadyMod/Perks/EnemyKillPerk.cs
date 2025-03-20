using GameNetcodeStuff;
using System.Collections.Generic;

namespace ShadyMod.Perks
{
    public class EnemyKillPerk : PerkBase
    {
        public override string Name => "Enemy Kill";

        public override string Description => "Kills all nearby enemys";

        public override string TriggerItemName => "lasse";

        public override bool CanPerkBeIncreased => false;

        public override void OnUpdate(PlayerControllerB player)
        {
            base.OnUpdate(player);

            if (StartOfRound.Instance.inShipPhase)
                return;

            var enemys = Helper.GetNearbyEnemys(player.transform.position, 5);
            if (enemys.Count > 0)
            {
                bool enemyFound = false;
                List<string> names = [];
                foreach (var enemy in enemys)
                {
                    ShadyMod.Logger.LogDebug($"Killing nearby enemy {enemy.name} ...");

                    if (enemy.dieSFX != null)
                        player.movementAudio.PlayOneShot(enemy.dieSFX);

                    enemy.KillEnemy();
                    names.Add(enemy.name.Replace("(Clone)", string.Empty).Replace("(clone)", string.Empty).Replace("Enemy", string.Empty));
                    
                    enemyFound = true;
                }

                if (enemyFound)
                {
                    Helper.DisplayTooltip($"Great job! You killed {string.Join(", ", names)}!");
                    player.DestroyItemInSlotAndSync(player.currentItemSlot);
                }
            }
        }
    }
}