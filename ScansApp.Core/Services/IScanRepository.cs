using System.Collections.Generic;
using ScansApp.Models;

namespace ScansApp.Services;

public interface IScanRepository
{
    IReadOnlyList<string> GetAvailableScanIds();

    Scan LoadScan(string scanId);
}
