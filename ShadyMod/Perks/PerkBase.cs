using GameNetcodeStuff;

namespace ShadyMod.Perks
{
    public abstract class PerkBase
    {
        protected bool isApplied = false;

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract string TriggerItemName { get; }

        public abstract bool CanPerkBeIncreased { get; }

        public virtual void Apply(PlayerControllerB player, bool force = false)
        {
            if (CanPerkBeIncreased && ShouldIncreasePerk(player))
                Helper.DisplayTooltip("Your own head? Great! Your perk will be increased!");
        }

        public virtual void Reset(PlayerControllerB player, bool force = false)
        {

        }

        public virtual void OnUpdate(PlayerControllerB player) { }    

        public virtual bool ShouldApply(PlayerControllerB player, GrabbableObject item)
        {
            return item.name.Contains(TriggerItemName); 
        }

        public virtual bool ShouldIncreasePerk(PlayerControllerB player)
        {
            return player.name.Contains(TriggerItemName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
