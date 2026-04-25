using GameNetcodeStuff;
using ShadyMod.Model;

namespace ShadyMod.Interactions
{
    public static class DonutInteraction
    {
        public static void ExecuteDonutInteraction(GrabbableObject currentItem, PlayerControllerB self, AssetInfo itemInfo)
        {
            if (itemInfo.ItemType == ItemType.BadDonut)
            {
                Helper.DisplayTooltip("That was a bad idea ...");
                //self.DamagePlayer(50, true, true);
                //self.DropAllHeldItems(true);
                self.KillPlayer(new UnityEngine.Vector3(0, 0), true, CauseOfDeath.Suffocation);
            }
            else
            {
                self.MakeCriticallyInjured(false);
                self.DestroyItemInSlotAndSync(self.currentItemSlot);
            }

            ShadyMod.DisablePerks(self);            
        }
    }
}