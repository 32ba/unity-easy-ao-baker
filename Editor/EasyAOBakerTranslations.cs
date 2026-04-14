using System.Collections.Generic;

namespace net._32ba.EasyAOBaker.Editor
{
    /// <summary>
    /// 翻訳データ。キー → (en, ja, zh, ko) のタプル。
    /// tooltip は "キー.tooltip" という命名規則で自動的に紐付く。
    /// </summary>
    internal static class Translations
    {
        public static readonly Dictionary<string, (string en, string ja, string zh, string ko)> Entries = new()
        {
            // Messages
            ["msg.no_renderer"] = (
                "This component requires a Renderer.\nAttach it to a GameObject that has a SkinnedMeshRenderer or MeshRenderer.",
                "このコンポーネントにはRendererが必要です。\nSkinnedMeshRenderer または MeshRenderer があるGameObjectに追加してください。",
                "此组件需要Renderer。\n请附加到具有SkinnedMeshRenderer或MeshRenderer的GameObject。",
                "이 컴포넌트에는 Renderer가 필요합니다.\nSkinnedMeshRenderer 또는 MeshRenderer가 있는 GameObject에 추가해 주세요."),
            ["msg.play_mode_preserve"] = (
                "Parameter changes made in Play Mode are automatically preserved after exiting Play Mode.",
                "Play Mode中のパラメータ変更は、Play Mode終了後に自動的に保持されます。",
                "在Play Mode期间的参数更改会在退出Play Mode后自动保留。",
                "Play Mode 중의 파라미터 변경은 Play Mode 종료 후 자동으로 유지됩니다."),
            ["msg.target_info"] = (
                "Target: {0} on '{1}'\nMode: {2} | Shader: {3}\nOn NDMF build, AO is baked considering the whole avatar's geometry.",
                "対象: {0} on '{1}'\nモード: {2} | シェーダー: {3}\nNDMFビルド時に、アバター全体のジオメトリを考慮してAOをベイクします。",
                "目标: {0} on '{1}'\n模式: {2} | 着色器: {3}\n在NDMF构建时，将考虑整个头像的几何体烘焙AO。",
                "대상: {0} on '{1}'\n모드: {2} | 셰이더: {3}\nNDMF 빌드 시 아바타 전체의 지오메트리를 고려하여 AO를 베이크합니다."),
            ["msg.vertex_color"] = (
                "Writes AO to the R channel of vertex colors.",
                "頂点カラーのRチャンネルにAOを書き込みます。",
                "将AO写入顶点颜色的R通道。",
                "정점 컬러의 R 채널에 AO를 씁니다."),
            ["msg.shader_not_detected"] = (
                "Could not auto-detect the shader. Please select Target Shader manually.",
                "シェーダーが自動検出できません。Target Shaderを手動で選択してください。",
                "无法自动检测着色器。请手动选择Target Shader。",
                "셰이더를 자동 감지할 수 없습니다. Target Shader를 수동으로 선택해 주세요."),

            // Sections
            ["section.bake_settings"] = ("Bake Settings", "ベイク設定", "烘焙设置", "베이크 설정"),
            ["section.advanced"] = ("Advanced Settings", "詳細設定", "高级设置", "고급 설정"),
            ["section.filter"] = ("Filter", "フィルタ", "滤镜", "필터"),
            ["section.shader_settings"] = ("Shader Settings", "シェーダー設定", "着色器设置", "셰이더 설정"),
            ["section.lil_toon"] = ("lilToon AO Settings", "lilToon AO設定", "lilToon AO设置", "lilToon AO 설정"),
            ["section.poiyomi"] = ("Poiyomi AO Settings", "Poiyomi AO設定", "Poiyomi AO设置", "Poiyomi AO 설정"),
            ["section.standard"] = ("Standard AO Settings", "Standard AO設定", "Standard AO设置", "Standard AO 설정"),

            // Basic
            ["field.resolution"] = ("Resolution", "解像度", "分辨率", "해상도"),
            ["field.resolution.tooltip"] = (
                "AO texture size. Higher values give more detail but use more memory.",
                "AOテクスチャのサイズ。大きいほど詳細だがメモリを多く消費する。",
                "AO贴图尺寸。越大细节越多但占用内存更多。",
                "AO 텍스처 크기. 클수록 세밀하지만 메모리를 더 많이 사용."),
            ["field.intensity"] = ("Intensity", "強度", "强度", "강도"),
            ["field.intensity.tooltip"] = (
                "Overall AO darkness multiplier (pow exponent).",
                "AO全体の強度倍率（pow指数）。",
                "AO整体强度倍率（pow指数）。",
                "AO 전체 강도 배율 (pow 지수)."),
            ["field.texture_only"] = (
                "Texture Only (manual)",
                "テクスチャのみ（手動ベイク）",
                "仅生成纹理（手动烘焙）",
                "텍스처만 (수동 베이크)"),
            ["field.texture_only.tooltip"] = (
                "Manual bake option. When enabled, the Bake AO Now button only outputs the PNG without modifying materials. NDMF build always applies to materials regardless of this setting.",
                "手動ベイク専用のオプション。ON のとき「Bake AO Now」ボタンは PNG を出力するだけでマテリアルは変更しない。NDMF ビルドはこの設定にかかわらず常にマテリアルへ適用。",
                "手动烘焙专用选项。启用时「Bake AO Now」按钮仅输出PNG，不修改材质。NDMF 构建无视此设置，始终应用到材质。",
                "수동 베이크 전용 옵션. 활성화 시 'Bake AO Now' 버튼은 PNG만 출력하고 머티리얼은 수정하지 않음. NDMF 빌드는 이 설정과 무관하게 항상 머티리얼에 적용."),
            ["field.target_shader"] = ("Target Shader", "対象シェーダー", "目标着色器", "대상 셰이더"),
            ["field.target_shader.tooltip"] = (
                "Shader slot to apply AO to. Auto detects from material.",
                "AOを適用するシェーダースロット。Autoでマテリアルから自動判定。",
                "AO应用的着色器槽。Auto会从材质自动检测。",
                "AO를 적용할 셰이더 슬롯. Auto는 머티리얼에서 자동 감지."),
            ["field.target_shader.detected"] = (
                "→ Detected: {0}",
                "→ 検出: {0}",
                "→ 检测到: {0}",
                "→ 감지: {0}"),
            ["field.target_shader.not_detected"] = (
                "Not detected",
                "検出できません",
                "无法检测",
                "감지 실패"),
            ["section.materials"] = (
                "Materials to bake",
                "AOを書き込むマテリアル",
                "烘焙目标材质",
                "AO를 쓸 머티리얼"),
            ["section.materials.tooltip"] = (
                "Toggle each slot to include/exclude from AO writing. Unchecked slots keep their original material unchanged.",
                "スロットごとに AO 書き込みの ON/OFF を切り替え。OFF のスロットはオリジナルマテリアルがそのまま残る。",
                "按槽位切换AO写入开关。关闭的槽位保留原始材质不变。",
                "슬롯별로 AO 쓰기 ON/OFF 전환. OFF인 슬롯은 원본 머티리얼이 그대로 유지됩니다."),
            ["materials.none_slot"] = ("(None)", "(なし)", "(无)", "(없음)"),
            ["field.ao_mask"] = ("AO Mask", "AOマスク", "AO遮罩", "AO 마스크"),
            ["field.ao_mask.tooltip"] = (
                "AO generation mask (white=generate, black=skip). Same UV space.",
                "AO生成マスク（白=生成する、黒=スキップ）。UVと同じ空間。",
                "AO生成遮罩（白=生成，黑=跳过）。UV空间相同。",
                "AO 생성 마스크 (흰색=생성, 검정=스킵). UV와 동일한 공간."),

            // Advanced: Bake Mode
            ["field.bake_mode"] = ("Bake Mode", "ベイクモード", "烘焙模式", "베이크 모드"),
            ["field.bake_mode.tooltip"] = (
                "SSAO: screen-space approximation. RayCast: physically accurate via BVH.",
                "SSAO: スクリーン空間近似。RayCast: BVHによる物理的に正確な計算。",
                "SSAO：屏幕空间近似。RayCast：通过BVH的物理精确计算。",
                "SSAO: 스크린 공간 근사. RayCast: BVH를 통한 물리적으로 정확한 계산."),

            // RayCast
            ["field.ray_count"] = ("Ray Count", "レイ数", "射线数", "레이 개수"),
            ["field.ray_count.tooltip"] = (
                "Rays cast per pixel. More = less noise, slower.",
                "ピクセルごとのレイ数。多いほどノイズ減少、遅くなる。",
                "每像素发射的射线数。越多噪点越少，速度越慢。",
                "픽셀당 발사되는 레이 수. 많을수록 노이즈 감소, 느려짐."),
            ["field.max_ray_distance"] = ("Max Ray Distance", "レイ最大距離", "射线最大距离", "레이 최대 거리"),
            ["field.max_ray_distance.tooltip"] = (
                "Ray length in meters. Shorter = local AO, longer = includes distant geometry.",
                "レイの長さ（m）。短いほど局所的、長いほど遠方の形状も考慮。",
                "射线长度（米）。越短越局部，越长考虑越远的几何体。",
                "레이 길이(m). 짧을수록 국소적, 길수록 먼 지오메트리까지 고려."),
            ["field.ray_origin_offset"] = ("Ray Origin Offset", "レイ原点オフセット", "射线起点偏移", "레이 원점 오프셋"),
            ["field.ray_origin_offset.tooltip"] = (
                "Distance from surface to start rays. Prevents self-intersection.",
                "レイ開始位置の面からの距離。自己交差を防ぐ。",
                "射线起点距表面的距离。防止自相交。",
                "레이 시작 위치의 표면으로부터의 거리. 자기 교차 방지."),

            // SSAO
            ["field.sample_count"] = ("Sample Count", "サンプル数", "采样数", "샘플 개수"),
            ["field.sample_count.tooltip"] = (
                "SSAO samples per pixel.",
                "SSAOのピクセルごとのサンプル数。",
                "SSAO每像素的采样数。",
                "SSAO 픽셀당 샘플 수."),
            ["field.radius"] = ("Radius", "半径", "半径", "반경"),
            ["field.radius.tooltip"] = (
                "SSAO sampling radius in meters.",
                "SSAOのサンプリング半径（m）。",
                "SSAO采样半径（米）。",
                "SSAO 샘플링 반경(m)."),
            ["field.bias"] = ("Bias", "バイアス", "偏置", "바이어스"),
            ["field.bias.tooltip"] = (
                "SSAO bias to prevent self-shadowing.",
                "自己シャドウを防ぐSSAOバイアス。",
                "防止自阴影的SSAO偏置。",
                "자기 그림자 방지를 위한 SSAO 바이어스."),
            ["field.camera_directions"] = ("Camera Directions", "カメラ方向数", "相机方向数", "카메라 방향 수"),
            ["field.camera_directions.tooltip"] = (
                "Number of depth map capture directions.",
                "深度マップ撮影方向の数。",
                "深度图捕获方向的数量。",
                "깊이 맵 캡처 방향의 수."),
            ["field.capture_distance"] = ("Capture Distance", "キャプチャ距離", "捕获距离", "캡처 거리"),
            ["field.capture_distance.tooltip"] = (
                "Depth camera distance from avatar.",
                "アバターから深度カメラまでの距離。",
                "深度相机距头像的距离。",
                "아바타로부터 깊이 카메라까지의 거리."),
            ["field.include_alpha_tested"] = ("Include Alpha Tested Meshes", "アルファテストメッシュを含める", "包含Alpha测试网格", "알파 테스트 메시 포함"),
            ["field.include_alpha_tested.tooltip"] = (
                "Use alpha-tested meshes as occluders (e.g., hair).",
                "アルファテストメッシュをオクルーダーとして使用（髪など）。",
                "将alpha测试网格用作遮挡物（例如头发）。",
                "알파 테스트 메시를 오클루더로 사용 (머리카락 등)."),

            // Filter
            ["field.blur_iterations"] = ("Blur Iterations", "ブラー反復回数", "模糊迭代次数", "블러 반복 횟수"),
            ["field.blur_iterations.tooltip"] = (
                "Number of blur passes applied to the AO map.",
                "AOマップに適用するブラー回数。",
                "应用于AO贴图的模糊次数。",
                "AO 맵에 적용되는 블러 횟수."),
            ["field.blur_radius"] = ("Blur Radius", "ブラー半径", "模糊半径", "블러 반경"),
            ["field.blur_radius.tooltip"] = (
                "Blur kernel radius in pixels.",
                "ブラーカーネルの半径（px）。",
                "模糊核的半径（像素）。",
                "블러 커널의 반경(픽셀)."),

            // lilToon Shadow Scale/Offset
            ["field.scale"] = ("  Scale", "  スケール", "  缩放", "  스케일"),
            ["field.scale.tooltip"] = (
                "AO strength applied to this shadow layer.",
                "このシャドウレイヤーへのAO適用強度。",
                "应用于此阴影层的AO强度。",
                "이 그림자 레이어에 적용되는 AO 강도."),
            ["field.offset"] = ("  Offset", "  オフセット", "  偏移", "  오프셋"),
            ["field.offset.tooltip"] = (
                "AO offset applied to this shadow layer.",
                "このシャドウレイヤーへのAOオフセット。",
                "应用于此阴影层的AO偏移。",
                "이 그림자 레이어에 적용되는 AO 오프셋."),

            ["field.post_ao"] = ("Post AO", "Post AO", "Post AO", "Post AO"),
            ["field.post_ao.tooltip"] = (
                "Ignore lilToon's Border property after applying AO.",
                "AO適用後にlilToonのBorderプロパティを無視する。",
                "AO应用后忽略lilToon的Border属性。",
                "AO 적용 후 lilToon의 Border 속성 무시."),
            ["field.border_mask_lod"] = ("Border Mask LOD", "Border Mask LOD", "Border Mask LOD", "Border Mask LOD"),
            ["field.border_mask_lod.tooltip"] = (
                "AO Map LOD level (same as lilToon Inspector value).",
                "AO MapのLODレベル（lilToon Inspector表示と同じ値）。",
                "AO贴图的LOD等级（与lilToon Inspector显示相同）。",
                "AO 맵의 LOD 레벨 (lilToon Inspector 표시와 동일)."),

            // Poiyomi (shared tooltip key)
            ["field.poi_r_strength"] = ("R Strength", "R 強度", "R 强度", "R 강도"),
            ["field.poi_g_strength"] = ("G Strength", "G 強度", "G 强度", "G 강도"),
            ["field.poi_b_strength"] = ("B Strength", "B 強度", "B 强度", "B 강도"),
            ["field.poi_a_strength"] = ("A Strength", "A 強度", "A 强度", "A 강도"),
            ["field.poi_strength.tooltip"] = (
                "AO strength for this channel.",
                "このチャンネルのAO強度。",
                "此通道的AO强度。",
                "이 채널의 AO 강도."),

            // Standard
            ["field.occlusion_strength"] = ("Occlusion Strength", "Occlusion強度", "Occlusion强度", "Occlusion 강도"),
            ["field.occlusion_strength.tooltip"] = (
                "Occlusion strength multiplier.",
                "Occlusion強度倍率。",
                "Occlusion强度倍率。",
                "Occlusion 강도 배율."),

            // Manual bake
            ["button.bake_now"] = ("Bake AO Now", "今すぐAOをベイク", "立即烘焙AO", "지금 AO 베이크"),
            ["button.bake_now.tooltip"] = (
                "Run the bake immediately without waiting for NDMF build. Outputs are saved under Assets/EasyAOBakerOutput/.",
                "NDMFビルドを待たずに即座にベイクを実行。結果は Assets/EasyAOBakerOutput/ 配下に保存されます。",
                "无需等待NDMF构建，立即执行烘焙。结果保存在 Assets/EasyAOBakerOutput/ 下。",
                "NDMF 빌드를 기다리지 않고 즉시 베이크를 실행. 결과는 Assets/EasyAOBakerOutput/ 아래에 저장됩니다."),
            ["dialog.ok"] = ("OK", "OK", "确定", "확인"),
            ["dialog.bake.title"] = ("Bake AO", "AOベイク", "烘焙AO", "AO 베이크"),
            ["dialog.no_avatar_root"] = (
                "Could not find the avatar root. Place this component under a VRC Avatar or run from a top-level GameObject.",
                "アバタールートが見つかりませんでした。VRChatアバター配下か、トップレベルのGameObject配下にコンポーネントを配置してください。",
                "找不到头像根节点。请将此组件放在VRChat头像下或顶级GameObject下。",
                "아바타 루트를 찾을 수 없습니다. 이 컴포넌트를 VRChat 아바타 아래 또는 최상위 GameObject 아래에 배치해 주세요."),
            ["dialog.bake.success"] = (
                "AO bake completed.\nOutput: {0}",
                "AOベイクが完了しました。\n出力先: {0}",
                "AO烘焙完成。\n输出位置: {0}",
                "AO 베이크 완료.\n출력 위치: {0}"),
            ["dialog.bake.failed"] = (
                "Bake failed: {0}",
                "ベイクに失敗しました: {0}",
                "烘焙失败: {0}",
                "베이크 실패: {0}"),
            ["progress.baking"] = ("Baking AO...", "AOをベイク中...", "正在烘焙AO...", "AO 베이크 중..."),

            // Update notification
            ["update.available"] = (
                "Update Available",
                "アップデートあり",
                "有可用更新",
                "업데이트 가능"),
            ["update.current_to_latest"] = (
                "Current: {0} → Latest: {1}",
                "現在: {0} → 最新: {1}",
                "当前: {0} → 最新: {1}",
                "현재: {0} → 최신: {1}"),
            ["update.open_release_page"] = (
                "Open Release Page",
                "リリースページを開く",
                "打开发布页面",
                "릴리스 페이지 열기"),
        };
    }
}
