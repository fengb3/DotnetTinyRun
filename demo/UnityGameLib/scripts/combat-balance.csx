// combat-balance.csx
// Run with: dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj -f ./demo/UnityGameLib/scripts/combat-balance.csx
// Purpose: Simulate battles between all character classes to check game balance

using UnityGameLib.Systems;

Console.WriteLine("=== RPG Combat Balance Simulation ===\n");

var classes = Enum.GetValues<CharacterClass>();
var party = classes.Select(c => Character.Create($"Player_{c}", c, level: 5)).ToArray();

// Show character stats
Console.WriteLine("Character Stats (Level 5):");
foreach (var c in party)
    Console.WriteLine($"  {c}");
    
Console.WriteLine();

// Run round-robin battles
Console.WriteLine("Battle Results (round-robin):");
var wins = new Dictionary<string, int>();
foreach (var c in party) wins[c.Class.ToString()] = 0;

for (int i = 0; i < party.Length; i++)
{
    for (int j = i + 1; j < party.Length; j++)
    {
        var a = party[i];
        var b = party[j];
        var result = CombatSimulator.Simulate(a, b);
        var winnerClass = party.First(p => p.Name == result.Winner).Class;
        wins[winnerClass.ToString()]++;
        Console.WriteLine($"  {a.Class,-10} vs {b.Class,-10}  => Winner: {result.Winner,-20} ({result.Rounds} rounds)");
    }
}

Console.WriteLine("\nWin Count:");
foreach (var (cls, count) in wins.OrderByDescending(kv => kv.Value))
    Console.WriteLine($"  {cls,-10} {count} wins");
