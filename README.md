# EasyAOBaker

VRChatアバター向けAOマップベイカー。NDMFプラグインとして動作し、AAOによるメッシュ統合・アトラス化後の最終ジオメトリに対してAOをベイクします。

## 特徴

- **非破壊ワークフロー**: NDMFパイプラインに統合、コンポーネントを追加するだけで自動適用
- **最終ジオメトリ準拠**: AAOのメッシュ統合・UV変更後のジオメトリでAOを計算
- **2つのベイクモード**:
  - **RayCast**（デフォルト）: BVH加速の物理ベースレイトレース、Hammersley低食い違い量列で高品質
  - **SSAO**: 多方向深度マップによるスクリーン空間近似、高速
- **シェーダー自動判別**: lilToon / Poiyomi / VRC Toon Standard / Unity Standard / Vertex Color
- **多言語対応**: English / 日本語 / 中文 / 한국어
- **AOマスク対応**: テクスチャでAO生成範囲を指定可能
- **GPU高速処理**: ComputeShaderによるレイトレース・ブラー・JFAパディング

## 動作要件

- Unity 2022.3 以降
- [NDMF](https://github.com/bdunderscore/ndmf) 1.9.0 以降

## インストール

### VPM（推奨）

1. VPMリポジトリ `https://vpm.32ba.net/` を追加
2. パッケージマネージャーから「EasyAOBaker」をインストール

### Git URL

Unity Package Manager の「Add package from git URL」で以下を入力:

```
https://github.com/32ba/easy-ao-baker.git
```

## 使い方

### 基本フロー

1. AOを焼きたいメッシュのGameObject（Rendererがあるオブジェクト）に **EasyAOBaker > EasyAOBaker** コンポーネントを追加
2. 必要に応じて設定を調整（Target Shader は Auto でほぼ自動判別）
3. アバターアップロード時、NDMFビルドパイプラインで自動ベイク → 対象シェーダーの AO スロットへ書き込み（非破壊）

### 手動ベイク

NDMFビルドを待たずに、Advanced Settings の **「Bake AO Now」** ボタンで即座にベイク可能。出力は `Assets/EasyAOBakerOutput/<アバター名>_<日時>/` 配下。

ボタンのすぐ上にある **「Texture Only (manual)」** トグル（デフォルト ON）で動作を切り替えられます:

- **ON**（デフォルト）: PNG のみ出力、マテリアルは変更しない。プレビューや単発のテクスチャ書き出し向け。生成された PNG を手動でマテリアルに割り当てる
- **OFF**: PNG + クローンした `_AO.mat` を出力し、Renderer の sharedMaterials を自動差し替え

NDMFビルドはこのトグルにかかわらず常にマテリアルに適用します（手動ベイク専用のオプションのため）。

複数メッシュにAOを焼く場合は、それぞれのGameObjectにコンポーネントを追加してください。アバター全体のジオメトリを考慮するため、他メッシュからの遮蔽（例: 髪→顔）も反映されます。

特定メッシュをオクルーダーから除外したい場合は **EasyAOBaker > Exclude From AO Bake** コンポーネントを追加してください。

### Inspector

UIは Basic / Advanced に分かれています。通常は Basic だけ触れば十分です。各項目にはマウスオーバーでツールチップが表示されます。

#### Basic（常時表示）

| 項目 | 説明 |
|---|---|
| Resolution | AOテクスチャのサイズ（256〜4096） |
| Intensity | AO全体の強度倍率（pow指数） |
| Target Shader | AO適用先のシェーダー。Auto推奨 |
| AO Mask | AO生成範囲のマスクテクスチャ（白=生成、黒=スキップ） |
| Shader Settings | シェーダー別の調整項目（lilToonの3シャドウ強度・オフセット、Poiyomiのチャンネル強度等） |

#### Advanced（折り畳み、デフォルト閉）

| 項目 | 説明 |
|---|---|
| Bake Mode | SSAO / RayCast の選択 |
| **RayCast 詳細** | |
| Ray Count | ピクセルあたりのレイ数。多いほどノイズ減少、遅くなる |
| Max Ray Distance | レイの最大長（m）。短いほど局所的、長いほど遠方も考慮 |
| Ray Origin Offset | レイ開始位置のオフセット。自己交差防止 |
| **SSAO 詳細** | |
| Sample Count | ピクセルあたりのサンプル数 |
| Radius | サンプリング半径（m） |
| Bias | 自己シャドウを防ぐバイアス |
| Camera Directions | 深度マップ撮影方向数 |
| Capture Distance | 深度カメラ距離 |
| Include Alpha Tested Meshes | アルファテストメッシュ（髪等）もオクルーダーに使う |
| **Filter** | |
| Blur Iterations | ブラー反復回数 |
| Blur Radius | ブラーカーネル半径 |

### 言語切り替え

Inspector右上のドロップダウンから English / 日本語 / 中文 / 한국어 に切り替え可能。設定は `EditorPrefs` に保存されます。

### Edit Mode プレビュー

Play Mode に入ると即座にAOがベイクされ、Edit Modeに戻った後もパラメータ変更は自動で保持されます。アップロード前のプレビューに便利です。

## ライセンス

[Zlib License](LICENSE)
