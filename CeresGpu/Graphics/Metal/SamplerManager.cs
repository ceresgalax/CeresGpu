using System;
using System.Collections.Generic;
using System.Numerics;
using CeresGpu.Graphics;

namespace Metalancer.Graphics.Metal;

sealed class SamplerManager : IDisposable
{
    // TODO: We could use a list instead and directly index.
    private readonly Dictionary<int, MetalSampler> _samplers = new();

    public SamplerManager(MetalRenderer renderer)
    {
        MakeSampler(renderer, MinMagFilter.Linear, MinMagFilter.Linear);
        MakeSampler(renderer, MinMagFilter.Linear, MinMagFilter.Nearest);
        MakeSampler(renderer, MinMagFilter.Nearest, MinMagFilter.Linear);
        MakeSampler(renderer, MinMagFilter.Nearest, MinMagFilter.Nearest);
    }

    public MetalSampler GetSampler(MinMagFilter min, MinMagFilter mag)
    {
        return _samplers[GetSamplerHash(min, mag)];
    }

    private void MakeSampler(MetalRenderer renderer, MinMagFilter min, MinMagFilter mag)
    {
        int hash = GetSamplerHash(min, mag);
        MetalSampler sampler = new (renderer, min, mag);
        _samplers.Add(hash, sampler);
    }
    
    private int GetSamplerHash(MinMagFilter min, MinMagFilter mag)
    {
        int minVal = (int)min;
        int baseVal = BitOperations.TrailingZeroCount((int)MinMagFilter.Max) + 1;
        int magVal = (int)mag * baseVal;
        return minVal + magVal;
    }

    public void Dispose()
    {
        foreach (MetalSampler sampler in _samplers.Values) {
            sampler.Dispose();
        }
    }
}