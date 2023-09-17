using System;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Task = Microsoft.Build.Utilities.Task;

namespace CeresGpuTasks;

public class FindPythonTask : Task
{
    [Output]
    public string PythonPath { get; set; } = "";

    private static string? CachedPythonPath;
    
    public override bool Execute()
    {
        if (CachedPythonPath != null) {
            PythonPath = CachedPythonPath;
            return true;
        }

        string[] pythons = { 
            "python3",
            "python"
        };

        foreach (string python in pythons) {
            if (IsPythonValid(python)) {
                PythonPath = python;
                CachedPythonPath = python;
                return true;
            }
        }
        
        Log.LogError($"Couldn't not find python on the PATH. Tried: {pythons}");
        return false;
    }
    
    private bool IsPythonValid(string python)
    {
        bool isPythonValid;
        
        Process process = new Process();
        process.StartInfo = new ProcessStartInfo {
            FileName = python,
            CreateNoWindow = true,
            Arguments = "--version",
            RedirectStandardOutput = true
        };
        process.Start();
        try {
            process.WaitForExit(1000 * 10);
            string outputLine = process.StandardOutput.ReadLine() ?? "";
            isPythonValid = outputLine.ToLowerInvariant().StartsWith("python 3");
        } finally {
            process.Kill();
        }

        return isPythonValid;
    }        
}
