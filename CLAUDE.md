# EasyAOBaker - CLAUDE.md

## プロジェクト概要

VRChatアバター向けAOマップベイカー。NDMFプラグインとして `BuildPhase.Optimizing` で AAO の後に実行し、処理済みメッシュに対してAOをベイクする。RayCast と SSAO の2モードを持つ。

## 技術スタック

- Unity 2022.3+
- NDMF >= 1.9.0 (Non-Destructive Modular Framework)
- ComputeShader (RayCast/SSAO計算、ガウシアンブラー、JFAパディング)
- BVH (RayCastモード用の空間分割)

## ディレクトリ構造

```
easy-ao-baker/
├── Editor/                              # Editor専用スクリプト
│   ├── EasyAOBakerPlugin.cs                 # NDMFプラグイン定義
│   ├── AOBakeProcessor.cs               # ベイク処理統括（SSAO + RayCast）
│   ├── BVHBuilder.cs                    # RayCastモード用BVH構築
│   ├── UVSpaceRasterizer.cs             # UV→ワールド座標マッピング
│   ├── DepthMapRenderer.cs              # SSAOモードの多方向深度レンダリング
│   ├── FibonacciSphere.cs               # 半球サンプリング
│   ├── ShaderAOSlotDetector.cs          # シェーダー別AO適用ロジック
│   ├── ShaderTypeUtil.cs                # シェーダー種別検出
│   ├── AOTexturePostFilter.cs           # ガウシアンブラー + JFAパディング
│   ├── EasyAOBakerAssetLoader.cs            # GUIDベースのアセットロード
│   ├── PlayModeParameterPersistence.cs  # Play Mode変更の永続化
│   ├── EasyAOBakerEditor.cs                 # カスタムInspector
│   ├── EasyAOBakerLocalization.cs           # 多言語対応API（L.Tr / L.G）
│   └── EasyAOBakerTranslations.cs           # 翻訳データ（en/ja/zh/ko）
├── Runtime/                             # ランタイムコンポーネント
│   ├── EasyAOBaker.cs                       # メインコンポーネント（ユーザー設定）
│   └── ExcludeFromAOBake.cs             # オクルーダー除外マーカー
├── Shaders/                             # GPU処理
│   ├── UVRasterize.shader               # UV空間ラスタライズ（保守的、膨張0）
│   ├── DepthOnly.shader                 # 深度のみ描画（SSAOモード用）
│   ├── RayCastBake.compute              # RayCast AO（Hammersley + PCG + BVH）
│   ├── SSAOBake.compute                 # SSAO AO（多方向深度ベース）
│   ├── AOBlur.compute                   # ガウシアンブラー（分離、7tap）
│   └── AOPadding.compute                # JFAベースUVパディング
├── package.json                         # VPMパッケージ定義
└── .github/workflows/release.yml
```

## コーディング規約

- **Namespace**: `net._32ba.EasyAOBaker` (Runtime), `net._32ba.EasyAOBaker.Editor` (Editor)
- **命名規則**: PascalCase (クラス/メソッド), `_camelCase` (privateフィールド), camelCase (ローカル変数)
- **Editor/Runtime分離**: Assembly Definitionで強制

## NDMFパイプライン統合

- `BuildPhase.Optimizing` で `AfterPlugin("com.anatawa12.avatar-optimizer")` として実行
- 処理後に自身のコンポーネント (EasyAOBaker, ExcludeFromAOBake) をクリーンアップ

## ベイクモード

### RayCast（デフォルト）

物理ベースの正確なAO計算。

1. アバター全体の三角形からBVH構築（CPU）
2. UV空間にラスタライズしてテクセルごとのワールド座標・法線を取得
3. 各テクセルから半球上にHammersley低食い違い量列でレイをサンプリング、PCGハッシュによるピクセルジッターでデコリレーション（Cranley-Patterson rotation）
4. BVHトラバースで遮蔽判定
5. ガウシアンブラー + JFAパディング

### SSAO

多方向深度マップによる近似AO計算。RayCastより高速だが精度は低い。

1. オクルーダーシーン構築（アバター全体のメッシュ）
2. Fibonacci球面分布でカメラ方向を決定
3. 各方向から深度マップ生成（Texture Array）
4. UV空間にラスタライズしたテクセルごとに、深度マップから遮蔽判定
5. ガウシアンブラー + JFAパディング

## ポストフィルタパイプライン

`AOTexturePostFilter.Apply()`:

1. 初回 JFA パディング（無効テクセル → 最近傍有効テクセルの値で埋める）
2. 分離型ガウシアンブラー（H → V を `blurIterations` 回）
3. 再 JFA パディング（ブラーで滲んだエッジを最近傍値で再埋め）

## 多言語対応

- 対応言語: English / 日本語 / 中文 / 한국어
- 翻訳データは `Editor/EasyAOBakerTranslations.cs` のキー → `(en, ja, zh, ko)` タプル辞書
- 命名規則 `"key.tooltip"` で tooltip を自動紐付け
- 言語選択は `EditorPrefs` キー `net.32ba.EasyAOBaker.Lang` に永続化、初回はシステム言語から自動判定
- API: `L.Tr(key)`, `L.G(labelKey)`（自動tooltip紐付け）, `L.G(labelKey, tooltipKey)`（明示）, `L.Format(key, args)`

## Inspector構成

- **Basic（常時表示）**: Resolution / Intensity / Target Shader / AO Mask / Shader Settings
- **Advanced（折り畳み）**: Bake Mode / モード別パラメータ / Filter

## 依存関係

- `nadena.dev.ndmf` >= 1.9.0 (vpmDependencies)

## リリース

- GitHub Actions `release.yml` (workflow_dispatch)
- package.jsonのversionからタグ・zip・unitypackage生成
- VPMリポジトリ (vpm.32ba.net) 自動更新
- リポジトリ変数 `PACKAGE_NAME` = `net.32ba.easy-ao-baker` の設定が必要
