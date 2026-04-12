# AO Baker

VRChatアバター向けSSAOベースAOマップベイカー。NDMFプラグインとして動作し、AAO等によるメッシュ統合・アトラス化後の最終ジオメトリに対して正確なAOをベイクします。

## 特徴

- **非破壊ワークフロー**: NDMFパイプラインに統合。コンポーネントを追加するだけで自動適用
- **最終ジオメトリ準拠**: AAOのメッシュ統合・UV変更後のジオメトリでAOを計算
- **シェーダー自動判別**: lilToon / Poiyomi / VRC Toon Standard / Unity Standard に対応
- **Edit Modeプレビュー**: ビルドを待たずに簡易プレビューが可能
- **GPU高速ベイク**: ComputeShaderによる多方向SSAO計算

## 動作要件

- Unity 2022.3 以降
- [NDMF](https://github.com/bdunderscore/ndmf) 1.9.0 以降

## インストール

### VPM (推奨)

1. VPMリポジトリ `https://vpm.32ba.net/` を追加
2. パッケージマネージャーから「AO Baker」をインストール

### Git URL

Unity Package Manager の「Add package from git URL」で以下を入力:

```
https://github.com/32ba/ao-baker.git
```

## 使い方

1. AOを焼きたいメッシュのGameObject（Rendererがあるオブジェクト）に「AO Baker > SSAO AO Baker」コンポーネントを追加
2. パラメータを調整（解像度、サンプル数、半径、強度等）
3. Play Mode進入 or アバターアップロード時に自動でAOがベイクされます

複数のメッシュにAOを焼きたい場合は、それぞれのGameObjectにコンポーネントを追加してください。深度マップはアバター全体のジオメトリから生成されるため、他メッシュからの遮蔽（例: 髪→顔）も反映されます。

特定のメッシュをオクルーダーから除外したい場合は、対象のGameObjectに「AO Baker > Exclude From AO Bake」コンポーネントを追加してください。

## ライセンス

[Zlib License](LICENSE)
