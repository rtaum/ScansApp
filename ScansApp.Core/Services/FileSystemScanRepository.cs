using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScansApp.Models;

namespace ScansApp.Services;

public sealed class FileSystemScanRepository : IScanRepository
{
    private readonly string scansRoot;

    public FileSystemScanRepository(string scansRoot)
    {
        this.scansRoot = scansRoot ?? throw new ArgumentNullException(nameof(scansRoot));
    }

    public IReadOnlyList<string> GetAvailableScanIds()
    {
        if (!Directory.Exists(scansRoot))
        {
            return Array.Empty<string>();
        }

        return Directory
            .GetDirectories(scansRoot)
            .Select(Path.GetFileName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToArray();
    }

    public Scan LoadScan(string scanId)
    {
        if (string.IsNullOrWhiteSpace(scanId))
        {
            throw new ArgumentException("Scan id is required.", nameof(scanId));
        }

        var scanDirectory = Path.Combine(scansRoot, scanId);
        if (!Directory.Exists(scanDirectory))
        {
            throw new DirectoryNotFoundException($"Scan '{scanId}' was not found.");
        }

        var planeADirectory = ResolvePlaneDirectory(scanDirectory, "Plane-A", "Plane_A");
        var planeBDirectory = ResolvePlaneDirectory(scanDirectory, "Plane-B", "Plane_B");

        var planeAImages = GetOrderedImages(planeADirectory);
        var planeBImages = GetOrderedImages(planeBDirectory);

        return new Scan(scanId, planeAImages, planeBImages);
    }

    private static string ResolvePlaneDirectory(string scanDirectory, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var path = Path.Combine(scanDirectory, candidate);
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException($"Required plane folder is missing in '{scanDirectory}'.");
    }

    private static IReadOnlyList<string> GetOrderedImages(string directory)
    {
        return Directory
            .GetFiles(directory, "*.png")
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
