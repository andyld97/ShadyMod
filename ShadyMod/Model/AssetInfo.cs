namespace ShadyMod.Model
{
    public class AssetInfo
    {
        // TODO: Rarity Balancing anpassen!

        public static readonly AssetInfo[] INSTANCE =
        [
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-belebt",
                Rarity = 70,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-paul",
                Rarity = 70,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-lasse",
                Rarity = 70,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-aveloth",
                Rarity = 70,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-andy",
                Rarity = 80,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,    
                Name = "head-jedon",
                Rarity = 70,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "head-patrick",
                Rarity = 70,
            },
            new AssetInfo()
            {
                Moons = LethalLib.Modules.Levels.LevelTypes.All,
                Name = "donout",
                Rarity = 30,
            },
        ];

        public string Name { get; set; } = string.Empty!;

        public int Rarity { get; set; } = 0;

        public LethalLib.Modules.Levels.LevelTypes Moons { get; set; } = LethalLib.Modules.Levels.LevelTypes.All;
    }
}