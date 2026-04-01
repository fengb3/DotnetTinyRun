namespace UnityGameLib.Systems;

/// <summary>Simple turn-based combat simulator for testing game balance.</summary>
public static class CombatSimulator
{
    private static readonly Random _rng = new(42); // fixed seed for reproducibility

    public record CombatResult(string Winner, int Rounds, List<string> Log);

    public static CombatResult Simulate(Character attacker, Character defender)
    {
        var log = new List<string>();
        var a = Clone(attacker);
        var b = Clone(defender);

        // Determine who goes first by Speed
        if (b.Speed > a.Speed) (a, b) = (b, a);

        int round = 0;
        while (a.IsAlive && b.IsAlive && round < 100)
        {
            round++;
            int dmg = System.Math.Max(1, a.Attack - b.Defense + _rng.Next(-3, 4));
            b.CurrentHp -= dmg;
            log.Add($"Round {round}: {a.Name} hits {b.Name} for {dmg} dmg (HP: {b.CurrentHp}/{b.MaxHp})");

            if (!b.IsAlive) break;
            (a, b) = (b, a);
        }

        var winner = a.IsAlive ? a.Name : b.Name;
        return new CombatResult(winner, round, log);
    }

    private static Character Clone(Character c) => new()
    {
        Name = c.Name, Class = c.Class, Level = c.Level,
        MaxHp = c.MaxHp, CurrentHp = c.CurrentHp,
        Attack = c.Attack, Defense = c.Defense, Speed = c.Speed,
        Skills = c.Skills
    };
}
