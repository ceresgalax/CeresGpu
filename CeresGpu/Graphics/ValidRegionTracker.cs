using System.Collections.Generic;

namespace CeresGpu.Graphics;

public class ValidRegionTracker
{
    /// <summary>
    /// start is the first index which becomes valid from this region.
    /// end is the first index which becomes invalid from this region.
    /// (start is inclusive, end is exclusive)
    /// </summary>
    private List<(uint start, uint end)> _validRegions = new();

    public void Reset()
    {
        _validRegions.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="start">The first index which becomes valid from this region.</param>
    /// <param name="end">The first index which becomes invalid from this region.</param>
    public void SetRegionValid(uint start, uint end)
    {
        // Find first region that the new region's start is before or within.

        int foundRegionIndex = _validRegions.Count;
        uint regionStart = 0, regionEnd = 0;
        
        for (int i = 0, ilen = _validRegions.Count; i < ilen; ++i) {
            (regionStart, regionEnd) = _validRegions[i];

            if (start <= regionEnd) {
                foundRegionIndex = i;
                break;
            }
        }

        if (foundRegionIndex == _validRegions.Count) {
            // This new region is after all other regions
            _validRegions.Add((start, end));
            return;
        }

        if (start < regionStart) {
            regionStart = start;
        }
        
        if (end > regionEnd) {
            regionEnd = end;
            
            // Find any regions after this region that should be engulfed
            int foundEndRegion = _validRegions.Count;
            
            for (int i = foundRegionIndex + 1, ilen = _validRegions.Count; i < ilen; ++i) {
                (uint nextRegionStart, uint nextRegionEnd) = _validRegions[i];
                if (end < nextRegionStart) {
                    foundEndRegion = i;
                    break;
                }
                
                regionEnd = nextRegionEnd;
            }
            
            _validRegions.RemoveRange(foundRegionIndex + 1, foundEndRegion - foundRegionIndex - 1);
        }

        // Update this region.
        _validRegions[foundRegionIndex] = (regionStart, regionEnd);
    }

    public IEnumerable<(uint start, uint count)> GetInvalidRegions(uint maxIndex)
    {
        uint currentStart = 0;
        foreach ((uint start, uint end) in _validRegions) {
            yield return (currentStart, currentStart - start);
            currentStart = end;
        }

        yield return (currentStart, currentStart - maxIndex);
    }

}