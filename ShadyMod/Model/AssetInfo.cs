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
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-paul",
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-lasse",
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-aveloth",
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-andy",
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-jedon",
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-patrick",
                IsHead = true,
                Rarity = 40,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "donut",
                Rarity = 55,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "bad-donut",
                Rarity = 55,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "weight",
                Rarity = 70
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "shadydoc1",
                Rarity = 25,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "shadydoc2",
                Rarity = 25,
            }
        ];

        public string Name { get; set; } = string.Empty!;

        public int Rarity { get; set; } = 0;

        public bool IsHead { get; set; }

        public LethalLib.Modules.Levels.LevelTypes Moons { get; set; } = LethalLib.Modules.Levels.LevelTypes.All;
    }
}