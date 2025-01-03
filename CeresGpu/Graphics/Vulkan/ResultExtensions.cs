using System;
using Silk.NET.Vulkan;

namespace CeresGpu.Graphics.Vulkan;

public static class ResultExtensions
{
    public static void AssertSuccess(this Result result, string failMessage)
    {
        if (result != Result.Success) {
            throw new InvalidOperationException(failMessage + ": " + result);
        }
    }
}