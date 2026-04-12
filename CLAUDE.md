# AO Baker - CLAUDE.md

## プロジェクト概要

VRChatアバター向けSSAOベースAOマップベイカー。NDMFプラグインとしてOptimizingフェーズでAAOの後に実行し、処理済みメッシュに対してAOをベイクする。

## 技術スタック

- Unity 2022.3+
- NDMF >= 1.9.0 (Non-Destructive Modular Framework)
- ComputeShader (SSAO計算、ブラー、パディング)

## ディレクトリ構造

```
ao-baker/
├── Editor/                     # Editor専用スクリプト
│   ├── SSAOBakerPlugin.cs      # NDMFプラグイン定義
│   ├── SSAOBakeProcessor.cs    # ベイク処理統括
│   ├── UVSpaceRasterizer.cs    # UV→ワールド座標マッピング
│   ├── DepthMapRenderer.cs     # 多方向深度レンダリング
│   ├── FibonacciSphere.cs      # 半球サンプリング
│   ├── ShaderAOSlotDetector.cs # シェーダー別AO適用
│   ├── AOTexturePostFilter.cs  # ブラー・パディング
│   └── SSAOBakerEditor.cs      # カスタムInspector
├── Runtime/                    # ランタイムコンポーネント
│   ├── SSAOBaker.cs            # メインコンポーネント（ユーザー設定）
│   └── ExcludeFromAOBake.cs    # 除外マーカー
├── Shaders/                    # GPU処理
│   ├── UVRasterize.shader      # UV空間ラスタライズ
│   ├── DepthOnly.shader        # 深度のみ描画
│   ├── SSAOBake.compute        # AO計算
│   ├── AOBlur.compute          # ブラーフィルタ
│   └── AOPadding.compute       # UVパディング
├── package.json                # VPMパッケージ定義
└── .github/workflows/release.yml
```

## コーディング規約

- **Namespace**: `net._32ba.AOBaker` (Runtime), `net._32ba.AOBaker.Editor` (Editor)
- **命名規則**: PascalCase (クラス/メソッド), `_camelCase` (privateフィールド), camelCase (ローカル変数)
- **Editor/Runtime分離**: Assembly Definitionで強制

## NDMFパイプライン統合

- `BuildPhase.Optimizing` で `AfterPlugin("com.anatawa12.avatar-optimizer")` として実行
- 処理後に自身のコンポーネント (SSAOBaker, ExcludeFromAOBake) をクリーンアップ

## ベイクパイプライン

1. Renderer収集 → 2. T-Poseジオメトリ取得 → 3. オクルーダーシーン構築
→ 4. 多方向深度レンダリング → 5. UV空間SSAO計算 → 6. ポストフィルタ → 7. マテリアル適用

## 依存関係

- `nadena.dev.ndmf` >= 1.9.0 (vpmDependencies)

## リリース

- GitHub Actions `release.yml` (workflow_dispatch)
- package.jsonのversionからタグ・zip・unitypackage生成
- VPMリポジトリ (vpm.32ba.net) 自動更新
- リポジトリ変数 `PACKAGE_NAME` = `net.32ba.ao-baker` の設定が必要
