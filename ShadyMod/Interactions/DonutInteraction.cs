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
                self.DamagePlayer(50, true, true);
                self.MakeCriticallyInjured(true);
            }
            else
                self.MakeCriticallyInjured(false);

            ShadyMod.DisablePerks(self);
            self.DestroyItemInSlotAndSync(self.currentItemSlot);
        }
    }
}