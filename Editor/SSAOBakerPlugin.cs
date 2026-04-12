using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(net._32ba.AOBaker.Editor.SSAOBakerPlugin))]

namespace net._32ba.AOBaker.Editor
{
    public class SSAOBakerPlugin : Plugin<SSAOBakerPlugin>
    {
        public override string QualifiedName => "net.32ba.ao-baker";
        public override string DisplayName => "AO Baker";

        protected override void Configure()
        {
            // Transformingフェーズの最後で実行
            // TTT・MA等のテクスチャ/メッシュ変更後、AAOの最適化前にAOをベイク
            InPhase(BuildPhase.Transforming)
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("nadena.dev.modular-avatar")
                .Run("Bake SSAO AO Maps", ctx =>
                {
                    var bakers = ctx.AvatarRootObject
                        .GetComponentsInChildren<SSAOBaker>(false)
                        .Where(b => b.gameObject.activeInHierarchy)
                        .ToArray();

                    if (bakers.Length == 0) return;

                    var processor = new SSAOBakeProcessor(ctx.AvatarRootObject, ctx);
                    processor.Execute(bakers);
                })
                .Then.Run("Cleanup AO Baker Components", ctx =>
                {
                    foreach (var comp in ctx.AvatarRootObject
                        .GetComponentsInChildren<SSAOBaker>(true))
                        Object.DestroyImmediate(comp);

                    foreach (var comp in ctx.AvatarRootObject
                        .GetComponentsInChildren<ExcludeFromAOBake>(true))
                        Object.DestroyImmediate(comp);
                });
        }
    }
}
