**[English](README.md)** | **[中文](README.zh-CN.md)**

# DotnetTinyRun

[![NuGet](https://img.shields.io/nuget/v/DotnetTinyRun)](https://www.nuget.org/packages/DotnetTinyRun)
[![NuGet Downloads](https://img.shields.io/nuget/dt/DotnetTinyRun)](https://www.nuget.org/packages/DotnetTinyRun)
[![License](https://img.shields.io/github/license/fengb3/DotnetTinyRun)](LICENSE)

轻量级 C# 脚本运行 CLI 工具 — 命令行版 LINQPad。

基于 [Roslyn](https://github.com/dotnet/roslyn) 脚本引擎，支持内联代码、脚本文件、stdin 管道输入，并能从 `.csproj` 加载完整的项目上下文（引用、using 指令、命名空间）。

## 安装

```bash
dotnet tool install --global DotnetTinyRun
```

## 使用

```bash
# 内联代码
dotnet-tiny-run "1 + 2"
dotnet-tiny-run "DateTime.Now.ToString(\"o\")"

# LINQPad 风格 .Dump()
dotnet-tiny-run "new[] { \"apple\", \"banana\", \"cherry\" }.Dump()"
dotnet-tiny-run "Enumerable.Range(1, 10).Where(x => x % 2 == 0).Dump()"

# 加载项目上下文（引用、using、命名空间）
dotnet-tiny-run -p ./src/MyApp/MyApp.csproj "DbContext.Users.Count()"

# 运行 .csx 脚本文件
dotnet-tiny-run -f script.csx

# 通过 stdin 管道输入
echo "DateTime.Now" | dotnet-tiny-run
```

## 选项

```
参数:
  CODE                      要执行的内联 C# 代码

选项:
  -p, --project <PATH>      指定 .csproj 路径以加载项目上下文
  -f, --file <PATH>         指定 .csx 脚本文件路径
  -u, --using <NS>          额外的 using 指令（可重复指定）
  -r, --reference <PATH>    额外的程序集引用（可重复指定）
      --no-default-imports  不添加默认 using 指令
      --debug               输出调试信息
  -h, --help                显示帮助
```

## 功能特性

### 默认导入

以下命名空间默认自动导入，无需手动添加 `using`：

`System`、`System.Linq`、`System.Collections`、`System.Collections.Generic`、`System.IO`、`System.Net.Http`、`System.Threading.Tasks`、`System.Text`、`System.Text.Json`

### .Dump() 扩展方法

提供了类似 LINQPad 的 `Dump()` 扩展方法，可用于任何对象：

```csharp
// 打印并返回值
42.Dump()
"hello".Dump("标签")

// 打印集合
new[] { 1, 2, 3 }.Dump()
```

### 项目上下文

使用 `--project` 时，DotnetTinyRun 会：

1. 构建项目
2. 解析所有 NuGet 和项目依赖
3. 从 `.csproj` 和 `GlobalUsings.cs` 提取 `using` 指令
4. 自动发现项目输出程序集中的命名空间
5. 将工作目录设置为项目输出路径

这让你可以直接对项目的完整类型系统执行单行代码：

```bash
dotnet-tiny-run -p ./MyWebApp/MyWebApp.csproj "new HttpClient().GetStringAsync(\"https://example.com\").Result.Length"
```

## 从源码构建

```bash
git clone https://github.com/fengb3/DotnetTinyRun.git
cd DotnetTinyRun
dotnet build src/DotnetTinyRun
```

## 许可证

MIT
