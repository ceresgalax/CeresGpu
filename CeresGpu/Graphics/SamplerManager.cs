using System;
using System.Collections.Generic;

namespace CeresGpu.Graphics;

/// <summary>
/// Utility class for fast lookup and creation of samplers for all possible sampler description permutationts.
/// </summary>
public sealed class SamplerManager : IDisposable
{
    private readonly Dictionary<SamplerDescription, ISampler> _samplers = new();

    public SamplerManager(IRenderer renderer)
    {
        void MakeSamplersForMinMagCombo(SamplerDescription baseDescription)
        {
            foreach (SamplerAddressMode depthMode in Enum.GetValues<SamplerAddressMode>()) {
                foreach (SamplerAddressMode widthMode in Enum.GetValues<SamplerAddressMode>()) {
                    foreach (SamplerAddressMode heightMode in Enum.GetValues<SamplerAddressMode>()) {
                        MakeSampler(renderer, baseDescription with {
                            DepthAddressMode = depthMode,
                            WidthAddressMode = widthMode,
                            HeightAddressMode = heightMode
                        });
                    }
                }
            }
        }
        
        foreach (MinMagFilter minValue in Enum.GetValues<MinMagFilter>()) {
            foreach (MinMagFilter magValue in Enum.GetValues<MinMagFilter>()) {
                MakeSamplersForMinMagCombo(new SamplerDescription {
                    MinFilter = minValue,
                    MagFilter = magValue
                });
            }
        }
    }

    public ISampler GetSampler(SamplerDescription description)
    {
        return _samplers[description];
    }

    private void MakeSampler(IRenderer renderer, SamplerDescription description)
    {
        ISampler sampler = renderer.CreateSampler(in description);
        _samplers.Add(description, sampler);
    }

    public void Dispose()
    {
        foreach (ISampler sampler in _samplers.Values) {
            sampler.Dispose();
        }
    }
}