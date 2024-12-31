using System.Collections.Generic;

namespace CeresGpu.Graphics;

public abstract class BaseTexture
{

    protected TextureLayout InitialLayout;

    // /// <summary>
    // /// Tracks the layout transitions caused by each pass this frame.
    // /// </summary>
    // protected readonly Dictionary<IPass, (TextureLayout inLayout, TextureLayout outLayout)> PassTransitions = [];
    
    /// <summary>
    /// Passes we have declared will transition the layout of the texture this frame.
    /// </summary>
    protected readonly Dictionary<IPass, (TextureLayout inLayout, TextureLayout outLayout)> DeclaredPassTransitions = [];

    /// <summary>
    /// Passes we have declared will mutate the texture this frame.
    /// </summary>
    protected readonly HashSet<IPass> DeclaredMutatingPasses = [];

    /// <summary>
    /// Passes we have declared to read from the texture this frame.
    /// </summary>
    protected readonly HashSet<IPass> DeclaredReadingPasses = [];
}