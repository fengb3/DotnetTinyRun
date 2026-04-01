// game-math.csx
// Run with: dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj -f ./demo/UnityGameLib/scripts/game-math.csx
// Purpose: Test and explore Unity-style Vector3 math operations

using UnityGameLib.Math;

Console.WriteLine("=== Vector3 Math Operations ===\n");

var origin = Vector3.Zero;
var target = new Vector3(3, 4, 0);

Console.WriteLine($"Origin:   {origin}");
Console.WriteLine($"Target:   {target}");
Console.WriteLine($"Distance: {Vector3.Distance(origin, target):F2}");
Console.WriteLine($"Dot:      {Vector3.Dot(origin, target):F2}");

var a = new Vector3(1, 0, 0);
var b = new Vector3(0, 1, 0);
Console.WriteLine($"\nCross({a}, {b}) = {Vector3.Cross(a, b)}");

// Linear interpolation — useful for movement and animations
Console.WriteLine("\n=== Lerp (linear interpolation) ===");
var start = new Vector3(0, 0, 0);
var end = new Vector3(10, 5, 0);
foreach (var t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
    Console.WriteLine($"  t={t:F2} => {Vector3.Lerp(start, end, t)}");

// Normalization
Console.WriteLine("\n=== Normalization ===");
var v = new Vector3(3, 4, 0);
Console.WriteLine($"  Vector:     {v}  (magnitude={v.Magnitude:F2})");
Console.WriteLine($"  Normalized: {v.Normalized}  (magnitude={v.Normalized.Magnitude:F2})");

// Common game math patterns
Console.WriteLine("\n=== Game Patterns ===");
var playerPos = new Vector3(0, 0, 0);
var enemyPos = new Vector3(5, 0, 3);
var direction = (enemyPos - playerPos).Normalized;
Console.WriteLine($"  Player -> Enemy direction: {direction}");
Console.WriteLine($"  Closing speed (dot with Forward): {Vector3.Dot(direction, Vector3.Forward):F2}");
