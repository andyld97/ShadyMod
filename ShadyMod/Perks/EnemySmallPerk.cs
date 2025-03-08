using GameNetcodeStuff;
using System;

namespace ShadyMod.Perks
{
    public class EnemySmallPerk : PerkBase
    {
        public override string Name => "Enemy Small Perk";

        public override string Description => "Scales all nearby enemies small";

        public override string TriggerItemName => "patrick";

        public override bool CanPerkBeIncreased => false;

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
                    ShadyMod.Logger.LogDebug($"#### Scaling enemy small: {enemy.name} ...");

                    float scaleFactor = .5f;
                    if (enemy.name.Contains("ForestGiant", StringComparison.OrdinalIgnoreCase))
                        scaleFactor = .15f;

                    enemy.transform.localScale = new UnityEngine.Vector3(scaleFactor, scaleFactor, scaleFactor);           
                }

                if (found)
                    player.DestroyItemInSlotAndSync(player.currentItemSlot);
            }
        }
    }
}