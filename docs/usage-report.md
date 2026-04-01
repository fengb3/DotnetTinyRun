# DotnetTinyRun 使用报告

**English** | [中文](#中文)

---

## Usage Report: ASP.NET & Unity Workflows

### Overview

This report documents a deep exploration of **DotnetTinyRun** from the perspective of two common .NET application domains: **ASP.NET Web API** development and **Unity-style game logic** development.

DotnetTinyRun functions as a CLI version of LINQPad — it lets you execute arbitrary C# one-liners or script files (`*.csx`) with optional access to a full project's type system, NuGet packages, and namespaces. This makes it ideal for exploration, debugging, and automation tasks alongside a running .NET project.

---

### 1. ASP.NET Web API — Usage Findings

**Demo project:** `demo/AspNetApp/` — a minimal Web API with `Product`, `Order`, and `OrderItem` models, an EF Core SQLite `AppDbContext`, and a `ProductService`.

#### What Works Well

**Inline type inspection** is frictionless:

```bash
# Inspect model type names at a glance
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj "typeof(AspNetApp.Models.Product).Name"
# → Product

# Count properties on a model
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj \
  "typeof(AspNetApp.Models.Order).GetProperties().Length"
# → 6
```

**Script files** make complex queries readable and repeatable:

```bash
# Full inventory report: grouping, low-stock alerts, total value
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj \
  -f ./demo/AspNetApp/scripts/query-products.csx
```
```
=== Inventory by Category ===
  Electronics     4 products  Value: ¤71,997.39
  Furniture       2 products  Value: ¤11,749.70
  Kitchen         2 products  Value: ¤2,423.43
  Stationery      2 products  Value: ¤6,392.00

=== Low Stock Alert (4 items) ===
  [  3 left] Headphones (Electronics) @ ¤199.99
  [  5 left] Standing Desk (Furniture) @ ¤599.99
  [  7 left] Water Bottle (Kitchen) @ ¤24.99
  [  8 left] USB Hub (Electronics) @ ¤49.99

Total Inventory Value: ¤92,562.52
```

**Order analytics** — with EF Core `Include()` for eager loading:

```bash
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj \
  -f ./demo/AspNetApp/scripts/order-analysis.csx
```
```
=== Order Summary ===
  #1 Alice Johnson        03/27/2026  Delivered    Total: ¤1,359.97
       - Laptop Pro x1 @ ¤1,299.99
       - Wireless Mouse x2 @ ¤29.99
  #2 Bob Smith            03/30/2026  Processing   Total: ¤949.98
  ...
Total Revenue: ¤2,373.87
```

**Reflection-based model inspection** — zero boilerplate code needed to explore your model schema:

```bash
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj \
  -f ./demo/AspNetApp/scripts/inspect-models.csx
```
```
[Product]
  Int32     Id
  String    Name
  String    Category
  Decimal   Price
  Int32     Stock
  Boolean   IsActive

[AppDbContext DbSets]
  DbSet<Product> -> Products
  DbSet<Order> -> Orders
  DbSet<OrderItem> -> OrderItems
```

#### Issues Encountered & Fixes Applied

| Issue | Root Cause | Fix |
|-------|-----------|-----|
| `Microsoft.Extensions.DependencyInjection.Abstractions` not found at runtime | ASP.NET SDK projects use shared framework (`Microsoft.AspNetCore.App`) assemblies that aren't copied to the build output or listed in `.deps.json` | `ProjectContextLoader` now parses `runtimeconfig.json` to discover and include shared framework directories |
| `libe_sqlite3.so` not found | SQLite native library lives in `runtimes/linux-x64/native/`, but .NET runtime probes only `AppContext.BaseDirectory` and the CoreLib directory | `ScriptRunner` now copies native libraries from `runtimes/<rid>/native/` into the project output directory before script execution |
| `System.Linq.Expressions.ParameterExpression` not found | `System.Linq.Expressions.dll` was missing from the default core references | Added `System.Linq.Expressions.dll` and `System.Linq.Queryable.dll` to the core reference set |

---

### 2. Unity-Style Game Logic — Usage Findings

> **Note on Unity compatibility:** Unity projects use a custom build system and the UnityEngine assemblies (bundled with the Unity Editor), which are not available as standard NuGet packages. Therefore, a standard `dotnet build` cannot produce a Unity game binary. However, **game logic code** — pure C# without UnityEngine dependencies — works perfectly with DotnetTinyRun. The `demo/UnityGameLib/` project simulates this pattern: a class library with Unity-inspired types (`Vector3`, `Transform`, `GameObject`, `Character`) that can be built and scripted with standard .NET tooling.

**Demo project:** `demo/UnityGameLib/` — a pure .NET class library with:
- `Math/Vector3` — 3D vector with distance, dot product, cross product, lerp, normalization
- `Core/Transform` — position, rotation (Euler angles), scale, `LookAt()`
- `Core/GameObject` — entity with named components
- `Systems/Character` — RPG character with class-based stats
- `Systems/CombatSimulator` — deterministic turn-based combat engine

#### What Works Well

**Vector math exploration** — ideal for testing game formulas interactively:

```bash
dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj \
  -f ./demo/UnityGameLib/scripts/game-math.csx
```
```
=== Vector3 Math Operations ===
Origin:   (0.00, 0.00, 0.00)
Target:   (3.00, 4.00, 0.00)
Distance: 5.00

=== Lerp (linear interpolation) ===
  t=0.00 => (0.00, 0.00, 0.00)
  t=0.25 => (2.50, 1.25, 0.00)
  t=0.50 => (5.00, 2.50, 0.00)
  t=0.75 => (7.50, 3.75, 0.00)
  t=1.00 => (10.00, 5.00, 0.00)

=== Game Patterns ===
  Player -> Enemy direction: (0.86, 0.00, 0.51)
```

**Combat balance simulation** — quickly identify stat imbalances without running the game:

```bash
dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj \
  -f ./demo/UnityGameLib/scripts/combat-balance.csx
```
```
=== RPG Combat Balance Simulation ===

Character Stats (Level 5):
  Player_Warrior [Warrior Lv5] HP:200/200 ATK:30 DEF:20 SPD:13
  Player_Mage    [Mage    Lv5] HP:110/110 ATK:50 DEF:9  SPD:15
  Player_Archer  [Archer  Lv5] HP:140/140 ATK:40 DEF:11 SPD:20
  Player_Rogue   [Rogue   Lv5] HP:120/120 ATK:42 DEF:10 SPD:23

Battle Results (round-robin):
  Warrior    vs Mage        => Winner: Player_Warrior  (12 rounds)
  Warrior    vs Archer      => Winner: Player_Warrior  (16 rounds)
  ...

Win Count:
  Warrior    3 wins
  Rogue      2 wins
  Archer     1 wins
  Mage       0 wins
```

> **Finding:** The Mage class never wins — useful feedback for a game designer without booting the Unity Editor.

**Scene graph inspection:**

```bash
dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj \
  -f ./demo/UnityGameLib/scripts/scene-inspection.csx
```
```
=== Scene Hierarchy ===
  GameObject 'Player' [active=True] at (0.00, 1.00, 0.00)
  GameObject 'Enemy_Goblin' [active=True] at (5.00, 0.00, 3.00)
  GameObject 'Enemy_Dragon' [active=True] at (10.00, 5.00, 8.00)

=== Combat Simulation: Player vs Nearest Enemy ===
  Nearest enemy: Enemy_Goblin at distance 5.9
  Battle result: Hero wins in 6 rounds

=== Transform Inspection ===
  Player looks at Enemy_Goblin:
    Transform { Position=(0.00, 1.00, 0.00), Rotation=(9.73, 59.04, 0.00), Scale=(1.00, 1.00, 1.00) }
```

#### Unity-Specific Limitations

| Limitation | Description |
|-----------|-------------|
| No UnityEngine dependency | `UnityEngine.dll` is not on NuGet; the tool cannot load actual Unity projects via `--project`. The workaround is to extract game logic into a standalone class library. |
| No Unity Build Pipeline | Unity projects use a custom build pipeline that cannot be triggered with `dotnet build`, so `ProjectContextLoader` cannot build them. |
| No MonoBehaviour lifecycle | Unity coroutines and MonoBehaviour lifecycle methods (`Start`, `Update`, etc.) cannot be tested without a Unity runtime. |

**Recommendation for Unity developers:** Extract game logic (formulas, AI, data models) into a separate `.NET Standard` or `.NET` class library project. This library can then be referenced from Unity **and** scripted with DotnetTinyRun for rapid testing.

---

### 3. General Observations

| Feature | Rating | Notes |
|---------|--------|-------|
| Inline one-liners | ⭐⭐⭐⭐⭐ | Instant, zero friction |
| `.csx` script files | ⭐⭐⭐⭐⭐ | Excellent for repeatable tasks |
| Project context loading | ⭐⭐⭐⭐ | Works well for console/library projects |
| ASP.NET SDK projects | ⭐⭐⭐ | Works after shared-framework fix |
| Native library projects | ⭐⭐⭐ | Works after native library copy fix |
| Unity projects | ⭐⭐ | Requires extracting logic to standalone library |
| `.Dump()` extension | ⭐⭐⭐⭐⭐ | Very convenient for data exploration |

#### Key Strengths
- **Zero boilerplate** — load your full project context with a single `-p` flag
- **Instant feedback** — great for data exploration, one-off migrations, and debugging
- **Works with EF Core** — query your database with the full LINQ API
- **Repeatable** — `.csx` script files act as mini-notebooks that can be version-controlled

#### Known Limitations
- Scripts that use native libraries require that those libraries are present in the platform's search path. The tool now handles this automatically.
- ASP.NET Core SDK projects (using `Microsoft.NET.Sdk.Web`) require the shared framework path resolution fix that was added in this PR.
- Very large projects with many transitive dependencies may have slower start times due to the `dotnet build` step.

---

## 中文

# DotnetTinyRun 使用报告：ASP.NET 与 Unity 应用场景

### 概述

本报告记录了从两个典型 .NET 应用开发角度对 **DotnetTinyRun** 的深度使用体验：**ASP.NET Web API** 开发场景 和 **Unity 风格游戏逻辑**开发场景。

DotnetTinyRun 是一个命令行版 LINQPad——你可以在不创建完整项目的情况下，直接运行 C# 内联代码或脚本文件（`*.csx`），并通过 `--project` 参数加载已有项目的完整类型系统、NuGet 包和命名空间。

---

### 1. ASP.NET Web API 场景

**演示项目：** `demo/AspNetApp/` — 包含 `Product`、`Order`、`OrderItem` 模型，使用 EF Core + SQLite 的 `AppDbContext`，以及 `ProductService`。

#### 使用效果

**内联类型查询** 无需任何样板代码：

```bash
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj "typeof(AspNetApp.Models.Product).Name"
# → Product
```

**脚本文件** 让复杂查询可复用：

```bash
dotnet-tiny-run -p ./demo/AspNetApp/AspNetApp.csproj -f ./demo/AspNetApp/scripts/query-products.csx
```

输出示例：
```
=== Inventory by Category ===
  Electronics     4 products  Value: ¤71,997.39
  Furniture       2 products  Value: ¤11,749.70

=== Low Stock Alert (4 items) ===
  [  3 left] Headphones (Electronics) @ ¤199.99

Total Inventory Value: ¤92,562.52
```

**支持 EF Core Include() 关联查询、分组统计、反射探查模型结构**——这些是日常 Web 开发中最常见的临时查询需求。

#### 遇到的问题及修复

| 问题 | 根因 | 修复方案 |
|------|------|--------|
| `Microsoft.Extensions.DependencyInjection.Abstractions` 运行时加载失败 | Web SDK 项目使用共享框架，相关 DLL 不在 `.deps.json` 中 | `ProjectContextLoader` 现在解析 `runtimeconfig.json` 来发现共享框架目录 |
| SQLite 原生库 `libe_sqlite3.so` 找不到 | 原生库在 `runtimes/linux-x64/native/` 子目录，.NET 运行时只探测 `AppContext.BaseDirectory` | `ScriptRunner` 现在会将原生库复制到输出目录根，确保平台加载器能找到它 |
| `System.Linq.Expressions` 编译错误 | 使用 EF Core `Include()` 需要 LINQ 表达式树，但该程序集未在默认引用中 | 在核心引用列表中添加了 `System.Linq.Expressions.dll` 和 `System.Linq.Queryable.dll` |

---

### 2. Unity 风格游戏逻辑场景

> **关于 Unity 的说明：** Unity 项目使用自定义构建管道，且 `UnityEngine.dll` 不在 NuGet 上，因此无法直接通过 `--project` 加载 Unity 项目。推荐的做法是：将游戏中的纯 C# 逻辑（数学计算、AI、数据模型）提取到独立的 .NET 类库项目中，这样既可以被 Unity 引用，又可以用 DotnetTinyRun 进行快速调试。

**演示项目：** `demo/UnityGameLib/` — 包含 `Vector3`、`Transform`、`GameObject`、`Character`、`CombatSimulator` 等 Unity 风格类型的纯 .NET 类库。

#### 使用效果

**向量数学探查** — 交互式验证游戏公式：

```bash
dotnet-tiny-run -p ./demo/UnityGameLib/UnityGameLib.csproj "new UnityGameLib.Math.Vector3(3,4,0).Magnitude"
# → 5
```

**战斗平衡仿真** — 不启动游戏引擎即可检测数值设计问题：

```
Win Count:
  Warrior    3 wins
  Rogue      2 wins
  Archer     1 wins
  Mage       0 wins    ← 法师从未胜利，数值需要调整
```

**场景图检查** — 探查运行时对象状态、Transform 计算：

```
Player looks at Enemy_Goblin:
  Transform { Position=(0.00,1.00,0.00), Rotation=(9.73,59.04,0.00), Scale=(1.00,1.00,1.00) }
```

---

### 3. 总结

DotnetTinyRun 在以下场景表现最佳：

- 🔍 **快速探查数据** — EF Core 查询、模型结构分析
- 🧪 **游戏逻辑测试** — 数值平衡、AI 算法、坐标计算
- 🔄 **一次性脚本任务** — 数据迁移、报表生成、批量修改
- 📖 **项目文档化** — 将探查脚本作为项目的"可运行文档"保存在版本控制中

**局限性：**

- Unity 完整项目（需要 UnityEngine）无法直接支持，需先提取纯 C# 逻辑为独立类库
- 使用原生库的项目需要工具正确配置原生库路径（本次 PR 已修复）
- ASP.NET Core SDK 项目需要共享框架路径解析（本次 PR 已修复）
