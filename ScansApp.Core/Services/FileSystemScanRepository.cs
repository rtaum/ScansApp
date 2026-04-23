using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ScansApp.Models;

namespace ScansApp.Services;

public sealed class FileSystemScanRepository : IScanRepository
{
    private static readonly ImageFileNameComparer ImageComparer = new();
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
            .OrderBy(static path => path, ImageComparer)
            .ToArray();
    }

    // Review feedback called out filename ordering specifically, so we compare
    // image names using natural numeric chunks instead of plain lexicographic order.
    private sealed class ImageFileNameComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var fileNameComparison = CompareNatural(Path.GetFileNameWithoutExtension(x), Path.GetFileNameWithoutExtension(y));
            return fileNameComparison != 0
                ? fileNameComparison
                : StringComparer.OrdinalIgnoreCase.Compare(x, y);
        }

        private static int CompareNatural(string? left, string? right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left is null)
            {
                return -1;
            }

            if (right is null)
            {
                return 1;
            }

            var leftIndex = 0;
            var rightIndex = 0;

            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                var leftIsDigit = char.IsDigit(left[leftIndex]);
                var rightIsDigit = char.IsDigit(right[rightIndex]);

                if (leftIsDigit && rightIsDigit)
                {
                    var leftNumber = ReadDigits(left, ref leftIndex);
                    var rightNumber = ReadDigits(right, ref rightIndex);
                    var digitComparison = CompareNumericText(leftNumber, rightNumber);
                    if (digitComparison != 0)
                    {
                        return digitComparison;
                    }

                    continue;
                }

                var leftText = ReadText(left, ref leftIndex);
                var rightText = ReadText(right, ref rightIndex);
                var textComparison = StringComparer.OrdinalIgnoreCase.Compare(leftText, rightText);
                if (textComparison != 0)
                {
                    return textComparison;
                }
            }

            return left.Length.CompareTo(right.Length);
        }

        private static string ReadDigits(string value, ref int index)
        {
            var start = index;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                index++;
            }

            return value[start..index];
        }

        private static string ReadText(string value, ref int index)
        {
            var builder = new StringBuilder();
            while (index < value.Length && !char.IsDigit(value[index]))
            {
                builder.Append(value[index]);
                index++;
            }

            return builder.ToString();
        }

        private static int CompareNumericText(string left, string right)
        {
            var leftTrimmed = left.TrimStart('0');
            var rightTrimmed = right.TrimStart('0');

            leftTrimmed = leftTrimmed.Length == 0 ? "0" : leftTrimmed;
            rightTrimmed = rightTrimmed.Length == 0 ? "0" : rightTrimmed;

            if (leftTrimmed.Length != rightTrimmed.Length)
            {
                return leftTrimmed.Length.CompareTo(rightTrimmed.Length);
            }

            var numericComparison = string.CompareOrdinal(leftTrimmed, rightTrimmed);
            return numericComparison != 0
                ? numericComparison
                : left.Length.CompareTo(right.Length);
        }
    }
}
