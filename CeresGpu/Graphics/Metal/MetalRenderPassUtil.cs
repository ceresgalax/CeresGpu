using System;
using CeresGpu.MetalBinding;

namespace CeresGpu.Graphics.Metal;

public class MetalRenderPassUtil
{
    public static MetalApi.MTLLoadAction TranslateLoadAction(LoadAction loadAction)
    {
        return loadAction switch {
            LoadAction.Load => MetalApi.MTLLoadAction.Load
            , LoadAction.Clear => MetalApi.MTLLoadAction.Clear
            , LoadAction.DontCare => MetalApi.MTLLoadAction.DontCare
            , _ => throw new ArgumentOutOfRangeException(nameof(loadAction), loadAction, null)
        };
    }
}