# ChromaMerge 実装計画

## 1. 概要

ChromaMergeは、CSSファイル内のカラーコードをCIEDE2000（ΔE00）色差アルゴリズムでグループ化し、
GUIで確認・マージできるAvalonia製デスクトップアプリケーションです。

### 1.1 現状

- Avalonia 11.3.9 + CommunityToolkit.Mvvm 8.2.1 のMVVMテンプレート
- **Phase 1 (Core Models) 完了**
  - `ColorCode` - カラーコード表現（#RGB, #RGBA, #RRGGBB, #RRGGBBAA 対応）
  - `LabColor` - CIE L*a*b* 色空間表現
  - `ColorConverter` - RGB ↔ Lab 変換（D65 標準光源）
  - `Ciede2000` - CIEDE2000 色差計算（公式テストデータ 34 ペア検証済み）
  - `UnionFind` - Union-Find データ構造（経路圧縮・ランク最適化）
- **テスト環境構築済み**
  - xUnit + FluentAssertions
  - 115 テストケース（全て合格）

### 1.2 目標機能（v0）

| 機能 | 説明 |
|------|------|
| フォルダスキャン | CSS/SCSS/SASS/LESS を再帰的に検索 |
| カラー抽出 | `#RGB`, `#RGBA`, `#RRGGBB`, `#RRGGBBAA` を抽出 |
| 色差計算 | CIEDE2000 (ΔE00) アルゴリズム |
| グルーピング | Union-Find で閾値以下の色をグループ化 |
| プレビュー | マージ前後の差分表示 |
| マージ適用 | バックアップ付きファイル置換 |

---

## 2. アーキテクチャ設計

### 2.1 レイヤー構成

```
ChromaMerge/
├── Models/                  # ドメインモデル・ビジネスロジック
│   ├── Color/               # 色関連
│   │   ├── ColorCode.cs     # カラーコード表現
│   │   ├── LabColor.cs      # Lab色空間表現
│   │   └── ColorConverter.cs # RGB↔Lab変換
│   ├── DeltaE/              # 色差計算
│   │   └── Ciede2000.cs     # CIEDE2000実装
│   ├── Grouping/            # グルーピング
│   │   └── UnionFind.cs     # Union-Find実装
│   ├── Scanning/            # ファイルスキャン
│   │   ├── FileScanner.cs   # ファイル検索
│   │   └── ColorExtractor.cs # カラーコード抽出
│   └── Merging/             # マージ処理
│       ├── MergePreview.cs  # プレビュー生成
│       └── FileMerger.cs    # ファイル書き換え
├── ViewModels/              # MVVM ViewModel
│   ├── MainWindowViewModel.cs
│   ├── ColorGroupViewModel.cs
│   └── MergePreviewViewModel.cs
├── Views/                   # MVVM View (AXAML)
│   ├── MainWindow.axaml
│   ├── ColorGroupView.axaml
│   └── MergePreviewView.axaml
└── Services/                # アプリケーションサービス
    └── DialogService.cs     # ダイアログ表示
```

### 2.2 依存関係

```
Views → ViewModels → Models
              ↓
          Services
```

### 2.3 データフロー

```
[フォルダ選択]
      ↓
[FileScanner] → CSS/SCSS/SASS/LESS ファイル一覧
      ↓
[ColorExtractor] → ColorOccurrence[] (色コード + 出現位置)
      ↓
[ColorConverter] → LabColor[] (Lab色空間に変換)
      ↓
[Ciede2000] → 色差行列 (全ペアのΔE00)
      ↓
[UnionFind] → ColorGroup[] (閾値でグルーピング)
      ↓
[GUI表示] ← ユーザー操作 → [マージ先選択]
      ↓
[MergePreview] → プレビュー生成
      ↓
[FileMerger] → バックアップ作成 → ファイル書き換え
```

---

## 3. 実装フェーズ

### Phase 1: Core Models（基盤）

#### 1.1 色表現モデル

**ColorCode.cs**
```csharp
public record ColorCode
{
    public string Original { get; init; }      // 元の文字列 (#fff, #ffffff等)
    public string Normalized { get; init; }    // 正規化形式 (#RRGGBBAA)
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; }
}
```

**LabColor.cs**
```csharp
public record LabColor(double L, double A, double B);
```

**ColorOccurrence.cs**
```csharp
public record ColorOccurrence
{
    public ColorCode Color { get; init; }
    public string FilePath { get; init; }
    public int LineNumber { get; init; }
    public int ColumnStart { get; init; }
    public int ColumnEnd { get; init; }
    public string Context { get; init; }       // 周辺テキスト（プロパティ宣言等）
}
```

#### 1.2 色変換

**ColorConverter.cs**
- `RgbToXyz()`: RGB → XYZ (D65照明)
- `XyzToLab()`: XYZ → Lab
- `RgbToLab()`: RGB → Lab (上記を組み合わせ)

参照: https://en.wikipedia.org/wiki/CIELAB_color_space

#### 1.3 CIEDE2000実装

**Ciede2000.cs**
```csharp
public static class Ciede2000
{
    public static double Calculate(LabColor lab1, LabColor lab2)
    {
        // CIEDE2000公式に基づく実装
        // 参照: http://www2.ece.rochester.edu/~gsharma/ciede2000/
    }
}
```

重み係数: `kL = kC = kH = 1.0`（デフォルト）

#### 1.4 Union-Find

**UnionFind.cs**
```csharp
public class UnionFind<T>
{
    public void Union(T a, T b);
    public T Find(T item);
    public IEnumerable<IGrouping<T, T>> GetGroups();
}
```

### Phase 2: Scanning（スキャン機能）

#### 2.1 ファイルスキャナー

**FileScanner.cs**
```csharp
public class FileScanner
{
    private static readonly string[] Extensions =
        { ".css", ".scss", ".sass", ".less" };

    public IAsyncEnumerable<string> ScanAsync(
        string rootPath,
        CancellationToken ct = default);
}
```

#### 2.2 カラー抽出

**ColorExtractor.cs**
```csharp
public class ColorExtractor
{
    // 正規表現パターン
    // #RGB, #RGBA, #RRGGBB, #RRGGBBAA
    private static readonly Regex HexColorPattern = new(
        @"#(?:[0-9A-Fa-f]{3,4}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\b",
        RegexOptions.Compiled);

    public IEnumerable<ColorOccurrence> Extract(string filePath);
}
```

### Phase 3: Grouping（グルーピング）

#### 3.1 グループ化ロジック

**ColorGrouper.cs**
```csharp
public class ColorGrouper
{
    public IReadOnlyList<ColorGroup> Group(
        IEnumerable<ColorOccurrence> occurrences,
        double threshold)
    {
        // 1. 全ユニーク色のLabを計算
        // 2. 全ペアのΔE00を計算
        // 3. 閾値以下のペアをUnion
        // 4. グループを返す
    }
}
```

**ColorGroup.cs**
```csharp
public record ColorGroup
{
    public int GroupId { get; init; }
    public IReadOnlyList<ColorOccurrence> Occurrences { get; init; }
    public ColorCode Representative { get; init; }  // 最頻出色
    public double MaxDeltaE { get; init; }          // グループ内最大ΔE
}
```

### Phase 4: UI（ユーザーインターフェース）

#### 4.1 メインウィンドウレイアウト

```
┌─────────────────────────────────────────────────────────────┐
│ [フォルダ選択] [パス表示                    ] [スキャン]    │
├─────────────────────────────────────────────────────────────┤
│ ΔE00しきい値: [====●=====] 2.5                              │
├───────────────────────┬─────────────────────────────────────┤
│ グループ一覧          │ 詳細                                │
│ ┌───────────────────┐ │ ┌─────────────────────────────────┐ │
│ │ ■ Group 1 (5色)  │ │ │ 色一覧:                         │ │
│ │ ■ Group 2 (3色)  │ │ │ ■ #FF0000  (12箇所)            │ │
│ │ ■ Group 3 (2色)  │ │ │ ■ #FF0102  (3箇所)             │ │
│ │                   │ │ │ ■ #FE0001  (1箇所)             │ │
│ │                   │ │ │                                 │ │
│ │                   │ │ │ 出現箇所:                       │ │
│ │                   │ │ │ src/style.css:12 (color:)      │ │
│ │                   │ │ │ src/button.scss:45 (bg:)       │ │
│ └───────────────────┘ │ └─────────────────────────────────┘ │
├───────────────────────┴─────────────────────────────────────┤
│ マージ先: [#FF0000 ▼]  [プレビュー]  [マージ適用]           │
└─────────────────────────────────────────────────────────────┘
```

#### 4.2 ViewModel設計

**MainWindowViewModel.cs**
```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string _folderPath = "";
    [ObservableProperty] private double _deltaEThreshold = 2.5;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private ColorGroupViewModel? _selectedGroup;

    public ObservableCollection<ColorGroupViewModel> Groups { get; } = new();

    [RelayCommand]
    private async Task SelectFolderAsync();

    [RelayCommand]
    private async Task ScanAsync();

    [RelayCommand]
    private void ShowPreview();

    [RelayCommand]
    private async Task ApplyMergeAsync();
}
```

**ColorGroupViewModel.cs**
```csharp
public partial class ColorGroupViewModel : ViewModelBase
{
    [ObservableProperty] private ColorCode? _selectedMergeTarget;

    public ColorGroup Model { get; }
    public IReadOnlyList<ColorOccurrenceViewModel> Occurrences { get; }
    public IReadOnlyList<ColorCode> UniqueColors { get; }
}
```

### Phase 5: Merge（マージ機能）

#### 5.1 プレビュー生成

**MergePreview.cs**
```csharp
public record MergeChange
{
    public string FilePath { get; init; }
    public int LineNumber { get; init; }
    public string Before { get; init; }
    public string After { get; init; }
}

public class MergePreviewGenerator
{
    public IReadOnlyList<MergeChange> Generate(
        ColorGroup group,
        ColorCode targetColor);
}
```

#### 5.2 マージ実行

**FileMerger.cs**
```csharp
public class FileMerger
{
    public async Task<MergeResult> ApplyAsync(
        IEnumerable<MergeChange> changes,
        bool createBackup = true,
        CancellationToken ct = default);
}

public record MergeResult
{
    public int FilesModified { get; init; }
    public int ReplacementsApplied { get; init; }
    public IReadOnlyList<string> BackupPaths { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
}
```

---

## 4. 詳細設計

### 4.1 CIEDE2000アルゴリズム

```
ΔE00 = √[(ΔL'/kL·SL)² + (ΔC'/kC·SC)² + (ΔH'/kH·SH)² + RT·(ΔC'/kC·SC)·(ΔH'/kH·SH)]
```

実装時の注意点:
- `atan2`の角度範囲処理（0-360度）
- 色相差のラップアラウンド処理
- 彩度が0の場合の特殊処理

参照実装: http://www2.ece.rochester.edu/~gsharma/ciede2000/

### 4.2 カラーコード正規化

| 入力 | 正規化後 |
|------|----------|
| `#RGB` | `#RRGGBBFF` |
| `#RGBA` | `#RRGGBBAA` |
| `#RRGGBB` | `#RRGGBBFF` |
| `#RRGGBBAA` | `#RRGGBBAA` |

例: `#f00` → `#FF0000FF`

### 4.3 Union-Findの閾値適用

```csharp
// 全ペアを比較（O(n²)）
for (int i = 0; i < colors.Count; i++)
{
    for (int j = i + 1; j < colors.Count; j++)
    {
        double deltaE = Ciede2000.Calculate(labs[i], labs[j]);
        if (deltaE <= threshold)
        {
            unionFind.Union(colors[i], colors[j]);
        }
    }
}
```

> **最適化検討**: 色数が多い場合はKD-Tree等の空間インデックスを検討

### 4.4 バックアップ戦略

1. 元ファイル: `style.css`
2. バックアップ: `style.css.bak`（同一ディレクトリ）
3. 複数回実行時: `style.css.bak.1`, `style.css.bak.2` ...

---

## 5. テスト戦略

### 5.1 テスト環境

- **フレームワーク**: xUnit
- **アサーション**: FluentAssertions
- **プロジェクト**: `ChromaMerge.Tests/`

### 5.2 ユニットテスト

| 対象 | テスト内容 | 状態 |
|------|------------|------|
| `ColorCode` | パース、正規化、等価性、null処理 | 35 tests |
| `ColorConverter` | RGB→Lab変換の精度、境界値、グレースケール | 14 tests |
| `Ciede2000` | 公開テストデータセットとの照合 | 37 tests |
| `UnionFind` | Union/Find/グループ化、コンストラクタ境界 | 17 tests |
| `Integration` | E2Eフロー、色グルーピングシミュレーション | 12 tests |
| `ColorExtractor` | 各フォーマットの抽出精度 | 未実装 |

### 5.3 CIEDE2000テストデータ

参照: http://www2.ece.rochester.edu/~gsharma/ciede2000/

公式テストデータセット（34ペア）を使用して精度検証 → **検証完了**

### 5.4 統合テスト

- サンプルCSSファイルセットでのE2Eテスト
- マージ前後のファイル内容検証
- バックアップ生成の確認

---

## 6. 将来の拡張（v1以降）

### 6.1 追加カラーフォーマット

- `rgb(r, g, b)` / `rgba(r, g, b, a)`
- `hsl(h, s%, l%)` / `hsla(h, s%, l%, a)`
- CSS Color Level 4: `color(display-p3 r g b)`
- CSS変数: `var(--color-primary)`

### 6.2 AST置換

正規表現置換ではなくASTベースの置換に移行:
- SCSSパーサー統合
- 安全な置換保証
- ネストされた色値の検出

### 6.3 パフォーマンス最適化

- 並列スキャン
- インクリメンタル更新
- 色差計算のキャッシュ

---

## 7. 実装順序

```
[x] Phase 1 (Core Models) ← 完了
  ├── [x] ColorCode, LabColor
  ├── [x] ColorConverter (RGB↔Lab)
  ├── [x] Ciede2000 (公式テストデータ検証済み)
  └── [x] UnionFind (経路圧縮・ランク最適化)

[ ] Phase 2-3 (Scanning + Grouping)
  ├── [ ] FileScanner
  ├── [ ] ColorExtractor
  ├── [ ] ColorOccurrence
  └── [ ] ColorGrouper

[ ] Phase 4 (UI)
  ├── [ ] MainWindow レイアウト
  ├── [ ] ViewModels
  └── [ ] データバインディング

[ ] Phase 5 (Merge + Polish)
  ├── [ ] MergePreview
  ├── [ ] FileMerger
  └── [ ] E2E テスト
```

---

## 8. 技術的決定事項

| 項目 | 決定 | 理由 |
|------|------|------|
| UIフレームワーク | Avalonia 11 | クロスプラットフォーム、WPF互換 |
| MVVMライブラリ | CommunityToolkit.Mvvm | Source Generator対応、軽量 |
| 色差アルゴリズム | CIEDE2000 | 人間の知覚に最も近い |
| グルーピング | Union-Find | シンプルかつ効率的 |
| ファイル検索 | System.IO | 標準ライブラリで十分 |
| 正規表現 | System.Text.RegularExpressions | v0では十分、将来AST化 |

---

## 9. リスクと対策

| リスク | 影響 | 対策 |
|--------|------|------|
| CIEDE2000実装の誤り | 色差が不正確 | 公式テストデータで検証 |
| 大量ファイルでの性能劣化 | UXの低下 | 非同期処理、プログレス表示 |
| 正規表現の誤マッチ | 誤置換 | コメント/文字列内を除外 |
| エンコーディング問題 | 文字化け | UTF-8を基本、BOM対応 |

---

## 付録A: 参照資料

- [CIEDE2000 Color-Difference Formula](http://www2.ece.rochester.edu/~gsharma/ciede2000/)
- [CIE Lab Color Space](https://en.wikipedia.org/wiki/CIELAB_color_space)
- [Avalonia UI Documentation](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
