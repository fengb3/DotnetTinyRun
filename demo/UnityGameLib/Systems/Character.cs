namespace UnityGameLib.Systems;

public enum CharacterClass { Warrior, Mage, Archer, Rogue }

/// <summary>RPG character with stats typical in Unity games.</summary>
public class Character
{
    public string Name { get; set; } = "";
    public CharacterClass Class { get; set; }
    public int Level { get; set; } = 1;
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public List<string> Skills { get; set; } = [];

    public bool IsAlive => CurrentHp > 0;

    public static Character Create(string name, CharacterClass cls, int level = 1) =>
        cls switch
        {
            CharacterClass.Warrior => new Character
            {
                Name = name, Class = cls, Level = level,
                MaxHp = 100 + level * 20, CurrentHp = 100 + level * 20,
                Attack = 15 + level * 3, Defense = 10 + level * 2, Speed = 8 + level,
                Skills = ["Slash", "Shield Bash", "Battle Cry"]
            },
            CharacterClass.Mage => new Character
            {
                Name = name, Class = cls, Level = level,
                MaxHp = 60 + level * 10, CurrentHp = 60 + level * 10,
                Attack = 25 + level * 5, Defense = 4 + level, Speed = 10 + level,
                Skills = ["Fireball", "Ice Lance", "Arcane Burst"]
            },
            CharacterClass.Archer => new Character
            {
                Name = name, Class = cls, Level = level,
                MaxHp = 80 + level * 12, CurrentHp = 80 + level * 12,
                Attack = 20 + level * 4, Defense = 6 + level, Speed = 15 + level,
                Skills = ["Arrow Shot", "Multishot", "Eagle Eye"]
            },
            CharacterClass.Rogue => new Character
            {
                Name = name, Class = cls, Level = level,
                MaxHp = 70 + level * 10, CurrentHp = 70 + level * 10,
                Attack = 22 + level * 4, Defense = 5 + level, Speed = 18 + level,
                Skills = ["Backstab", "Poison Blade", "Vanish"]
            },
            _ => throw new ArgumentOutOfRangeException()
        };

    public override string ToString() =>
        $"{Name} [{Class} Lv{Level}] HP:{CurrentHp}/{MaxHp} ATK:{Attack} DEF:{Defense} SPD:{Speed}";
}
