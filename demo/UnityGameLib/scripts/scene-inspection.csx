// scene-inspection.csx
// Run with: dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj -f ./demo/UnityGameLib/scripts/scene-inspection.csx
// Purpose: Demonstrate building a scene graph and inspecting GameObjects — mirrors Unity workflows

using UnityGameLib.Core;
using UnityGameLib.Math;
using UnityGameLib.Systems;

// Build a simple scene
var player = new GameObject("Player");
player.Transform.Position = new Vector3(0, 1, 0);
player.AddComponent<Character>(); // attach RPG component
var playerChar = player.GetComponent<Character>()!;
playerChar.Name = "Hero";
playerChar.Class = CharacterClass.Warrior;
playerChar.Level = 10;
playerChar.MaxHp = playerChar.CurrentHp = 300;
playerChar.Attack = 45;
playerChar.Defense = 30;

var enemy1 = new GameObject("Enemy_Goblin");
enemy1.Transform.Position = new Vector3(5, 0, 3);
enemy1.AddComponent<Character>();
var goblin = enemy1.GetComponent<Character>()!;
goblin.Name = "Goblin";
goblin.Class = CharacterClass.Rogue;
goblin.Level = 3;
goblin.MaxHp = goblin.CurrentHp = 80;
goblin.Attack = 25;
goblin.Defense = 8;
goblin.Speed = 20;

var enemy2 = new GameObject("Enemy_Dragon");
enemy2.Transform.Position = new Vector3(10, 5, 8);
enemy2.AddComponent<Character>();
var dragon = enemy2.GetComponent<Character>()!;
dragon.Name = "Dragon";
dragon.Class = CharacterClass.Mage;
dragon.Level = 20;
dragon.MaxHp = dragon.CurrentHp = 800;
dragon.Attack = 120;
dragon.Defense = 60;

var scene = new[] { player, enemy1, enemy2 };

Console.WriteLine("=== Scene Hierarchy ===");
foreach (var obj in scene)
    Console.WriteLine($"  {obj}");

Console.WriteLine("\n=== Combat Simulation: Player vs Nearest Enemy ===");
var nearest = scene.Skip(1).MinBy(e => Vector3.Distance(player.Transform.Position, e.Transform.Position))!;
Console.WriteLine($"  Nearest enemy: {nearest.Name} at distance {Vector3.Distance(player.Transform.Position, nearest.Transform.Position):F1}");

var nearestChar = nearest.GetComponent<Character>()!;
var result = CombatSimulator.Simulate(playerChar, nearestChar);
Console.WriteLine($"  Battle result: {result.Winner} wins in {result.Rounds} rounds");
Console.WriteLine("\n  Battle log (last 5 rounds):");
foreach (var entry in result.Log.TakeLast(5))
    Console.WriteLine($"    {entry}");

Console.WriteLine("\n=== Transform Inspection ===");
player.Transform.LookAt(nearest.Transform.Position);
Console.WriteLine($"  Player looks at {nearest.Name}: {player.Transform}");
