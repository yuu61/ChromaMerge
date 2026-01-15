# ChromaMerge

**ChromaMerge** は、リポジトリ内に散在する CSS カラーコードを
**CIEDE2000（ΔE00）による知覚色差**でグループ化し、
GUI 上で確認・選択しながら安全にマージできるデスクトップツールです。

Avalonia + .NET で実装されており、**インストール不要の単一実行ファイル**として実行できます。

### Cross-Platform

Windows, macOS, Linux

.NET 8 + Avalonia により、**すべての主要プラットフォームでネイティブ動作**します。
各 OS 向けに単一実行ファイル（self-contained）として配布可能です。

## Features

- 📁 **フォルダ選択 → 自動スキャン**
- 🎚 **ΔE00（CIEDE2000）スライダー**で色差しきい値を動的に変更
- 🎨 **近似色の自動グルーピング（Union-Find）**
- 🔍 グループ単位で
  - 色一覧
  - 出現回数
  - 出現箇所（ファイル / 行 / 宣言）
  を確認可能
- 🔁 **マージ先を選択してプレビュー**
- ✅ 確認後に **安全にマージ適用**
  - 自動バックアップ（`.bak`）生成

## Why ChromaMerge?

- 単純な文字列一致ではなく
  **人間の知覚に近い色差（ΔE00）** を使用
- デザイン・実装どちらの視点でも
  「本当に同じ色か？」を判断しやすい
- CI や lint 以前の
  **既存リポジトリの色の整理・棚卸し**に最適

## Supported Color Formats (v0)

- `#RGB`
- `#RGBA`
- `#RRGGBB`
- `#RRGGBBAA`

> `rgb() / hsl() / gradient / shadow` 等は今後対応予定
> （内部設計は AST 置換を前提に拡張可能）

## How It Works

1. フォルダを選択
2. CSS / SCSS / SASS / LESS を再帰的にスキャン
3. カラーコードを正規化（`#RRGGBBAA`）
4. RGB → Lab 変換
5. **CIEDE2000 (ΔE00)** で色差を計算
6. しきい値以下の色をグループ化
7. GUI で確認・マージ

## ΔE00 Reference

|   ΔE00 | Meaning      |
| -----: | ------------ |
|    ≤ 1 | ほぼ識別不能 |
|  1 – 2 | 非常に近い   |
|  2 – 5 | 近似色       |
| 5 – 10 | 明確に異なる |
|   > 10 | 別色         |

UI デザイン用途では **2.0〜3.0** が実用的な初期値です。

## Development Status

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1: Core Models | Done | ColorCode, LabColor, ColorConverter, Ciede2000, UnionFind |
| Phase 2: Scanning | - | FileScanner, ColorExtractor |
| Phase 3: Grouping | - | ColorGrouper |
| Phase 4: UI | - | MainWindow, ViewModels |
| Phase 5: Merge | - | MergePreview, FileMerger |

## Build & Run

### Requirements
- .NET 8 SDK

### Run (Development)

```bash
dotnet run
```

### Run Tests

```bash
dotnet test
```

115 tests including official CIEDE2000 test dataset (34 pairs).

### Build (Release)

各プラットフォーム向けに単一実行ファイルを生成：

```bash
# Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Windows (arm64)
dotnet publish -c Release -r win-arm64 --self-contained -p:PublishSingleFile=true

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true

# Linux (x64)
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# Linux (arm64)
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=true
```

出力先: `bin/Release/net8.0/<RID>/publish/`
