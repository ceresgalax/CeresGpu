using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

class PoolInfo(DescriptorPool pool, int capacitySets, int capacityDescriptorsPerType, Dictionary<DescriptorType, int> usedDescriptorsByType)
{
    public DescriptorPool Pool = pool;
    public int CapacitySets = capacitySets;
    public int UsedSets;
    public int CapacityDescriptorsPerType = capacityDescriptorsPerType;
    public Dictionary<DescriptorType, int> UsedDescriptorsByType = usedDescriptorsByType;
}

public sealed class DescriptorPoolManager : IDisposable
{
    private const int MAX_DESCRIPTORS_PER_TYPE = 512;
    
    private readonly VulkanRenderer _renderer;
    private readonly DescriptorPoolSize[] _poolSizes;
    
    private readonly List<PoolInfo> _pools = [];
    private readonly HashSet<int> _poolsWithVacantSets = [];
    private readonly Dictionary<DescriptorType, HashSet<int>> _poolsWithVacantDescriptors = [];
    private readonly Dictionary<DescriptorPool, int> _poolIndices = [];

    public DescriptorPoolManager(VulkanRenderer renderer, DescriptorType[] descriptorTypes)
    {
        _renderer = renderer;
        
        _poolSizes = descriptorTypes
            .Select(descriptorType => new DescriptorPoolSize(descriptorType, MAX_DESCRIPTORS_PER_TYPE))
            .ToArray();

        _poolsWithVacantDescriptors = descriptorTypes.ToDictionary(dt => dt, _ => new HashSet<int>());
    }
    
    private void ReleaseUnmanagedResources()
    {
        foreach (PoolInfo info in _pools) {
            // TODO: Do we need to release the pools first?
            unsafe {
                _renderer.Vk.DestroyDescriptorPool(_renderer.Device, info.Pool, null);    
            }
            info.Pool = default;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~DescriptorPoolManager()
    {
        ReleaseUnmanagedResources();
    }

    private readonly HashSet<int> _tempA = [];
    
    public DescriptorSet AllocateDescriptorSet(DescriptorSetLayout layout, ReadOnlySpan<(DescriptorType type, int count)> descriptorCounts, out DescriptorPool poolAllocatedFrom)
    {
        _tempA.Clear();

        if (descriptorCounts.Length == 0) {
            throw new ArgumentException("descriptorCounts length must be greater than 0.", nameof(descriptorCounts));
        }
        
        HashSet<int> candidatePools = _tempA;
        candidatePools.UnionWith(_poolsWithVacantSets);
        for (int i = 0; i < descriptorCounts.Length; i++) {
            if (_poolsWithVacantDescriptors.TryGetValue(descriptorCounts[i].type, out HashSet<int>? pools)) {
                candidatePools.IntersectWith(pools);    
            }
        }

        DescriptorSet descriptorSet;
        
        foreach (int candidatePoolIndex in candidatePools) {
            Result result = AllocateDescriptorSetFromPool(candidatePoolIndex, layout, descriptorCounts, out poolAllocatedFrom, out descriptorSet);
            if (result != Result.ErrorFragmentedPool) {
                result.AssertSuccess("Failed to allocate descriptorSet from existing pool");
                return descriptorSet;
            }
        }
        
        // Time for a new pool!
        int newPoolIndex = CreateNewPool();
        AllocateDescriptorSetFromPool(newPoolIndex, layout, descriptorCounts, out poolAllocatedFrom, out descriptorSet)
            .AssertSuccess("Failed to allocate descriptorSet from a brand new pool.");
        return descriptorSet;
    }

    public void FreeDescriptorSet(DescriptorSet descriptorSet, DescriptorPool pool, ReadOnlySpan<(DescriptorType type, int count)> descriptorCounts)
    {
        _renderer.Vk.FreeDescriptorSets(_renderer.Device, pool, 1, in descriptorSet)
            .AssertSuccess("Failed to free descriptorSet.");
        
        // Update our vacancy bookkeeping
        int poolIndex = _poolIndices[pool];
        PoolInfo info = _pools[poolIndex];
        
        if (info.UsedSets == info.CapacitySets) {
            _poolsWithVacantSets.Remove(poolIndex);
        }
        --info.UsedSets;

        foreach ((DescriptorType type, int count) in descriptorCounts) {
            int used = info.UsedDescriptorsByType[type];
            if (used == info.CapacityDescriptorsPerType) {
                _poolsWithVacantDescriptors[type].Remove(poolIndex);
            }
            info.UsedDescriptorsByType[type] = used - count;
        }
    }

    private unsafe int CreateNewPool()
    {
        const int maxDescriptorSets = 512;

        DescriptorPool pool;
        
        fixed (DescriptorPoolSize* pSizes = _poolSizes) {
            DescriptorPoolCreateInfo createInfo = new(
                sType: StructureType.DescriptorPoolCreateInfo,
                pNext: null,
                flags: DescriptorPoolCreateFlags.FreeDescriptorSetBit, // TODO: Do we need CreateUpdateAfterBindBit?
                maxSets: maxDescriptorSets,
                poolSizeCount: (uint)_poolSizes.Length,
                pPoolSizes: pSizes
            );
            _renderer.Vk.CreateDescriptorPool(_renderer.Device, in createInfo, null, out pool)
                .AssertSuccess("Failed to create descriptor pool");    
        }

        PoolInfo info = new PoolInfo(pool, maxDescriptorSets, MAX_DESCRIPTORS_PER_TYPE, _poolSizes.ToDictionary(ps => ps.Type, _ => 0));

        int index = _pools.Count;
        _pools.Add(info);
        _poolIndices[pool] = index;
        _poolsWithVacantSets.Add(index);
        foreach (DescriptorPoolSize size in _poolSizes) {
            _poolsWithVacantDescriptors[size.Type].Add(index);
        }
        return index;
    }

    private Result AllocateDescriptorSetFromPool(int poolIndex, DescriptorSetLayout layout, ReadOnlySpan<(DescriptorType type, int count)> descriptorCounts, out DescriptorPool poolAllocatedFrom, out DescriptorSet descriptorSet)
    {
        PoolInfo poolInfo = _pools[poolIndex];
        poolAllocatedFrom = poolInfo.Pool;
        
        Result result;
        unsafe {
            DescriptorSetAllocateInfo allocateInfo = new DescriptorSetAllocateInfo(
                sType: StructureType.DescriptorSetAllocateInfo,
                pNext: null,
                descriptorPool: poolInfo.Pool,
                descriptorSetCount: 1,
                &layout
            );
            result = _renderer.Vk.AllocateDescriptorSets(_renderer.Device, in allocateInfo, out descriptorSet);
        }

        if (result == Result.Success) {
            // Update our vacancy bookkeeping

            ++poolInfo.UsedSets;
            if (poolInfo.UsedSets == poolInfo.CapacitySets) {
                _poolsWithVacantSets.Remove(poolIndex);
            }
            
            foreach ((DescriptorType type, int count) in descriptorCounts) {
                int used = poolInfo.UsedDescriptorsByType[type];
                used += count;
                poolInfo.UsedDescriptorsByType[type] = used;

                if (used == poolInfo.CapacityDescriptorsPerType) {
                    _poolsWithVacantDescriptors[type].Remove(poolIndex);
                }
            }
        }

        return result;
    }
}
