using System.Linq;

namespace ShadyMod.Model
{
    public class AssetInfo
    {
        public static readonly AssetInfo[] INSTANCE =
        [
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-belebt",
                PrefabName = "belebt",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-paul",
                PrefabName = "paul",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-lasse",
                PrefabName = "lasse",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-aveloth",
                PrefabName = "aveloth",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-andy",
                PrefabName = "andy",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-jedon",
                PrefabName = "jedon",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-patrick",
                PrefabName = "patrick",
                ItemType = ItemType.McHead,
                Rarity = 35,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "donut",
                PrefabName = "donut",
                ItemType = ItemType.Donut,
                Rarity = 55,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "bad-donut",
                PrefabName = "donut-bad",
                ItemType = ItemType.BadDonut,
                Rarity = 55,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "weight",
                PrefabName = "weight",
                ItemType = ItemType.Weight,
                Rarity = 65
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "shadydoc1",
                PrefabName = "ShadyDocument1",
                ItemType = ItemType.ShadyDocument,
                Rarity = 25,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "shadydoc2",
                PrefabName = "ShadyDocument2",
                ItemType = ItemType.ShadyDocument,
                Rarity = 25,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "robot",
                PrefabName = "robot",
                ItemType = ItemType.Robot,
                Rarity = 15
            }
        ];

        public string Name { get; set; } = string.Empty!;

        public string PrefabName { get; set; } = string.Empty!;

        public int Rarity { get; set; } = 0;

        public ItemType ItemType { get; set; }

        public LethalLib.Modules.Levels.LevelTypes Moons { get; set; } = LethalLib.Modules.Levels.LevelTypes.All;

        public static bool IsShadyItem(string name)
        {
            return GetShadyNameByName(name) != null;
        }

        public static AssetInfo GetShadyNameByName(string name)
        {
            string searchName = name.ToLower().Replace("(clone)", string.Empty);
            return AssetInfo.INSTANCE.FirstOrDefault(p => p.PrefabName.ToLower() == searchName);
        }
    }

    public enum ItemType
    {
        McHead,
        Donut,
        BadDonut,
        Weight,
        ShadyDocument,
        Robot
    }
}