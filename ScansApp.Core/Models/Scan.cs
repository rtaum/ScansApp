using System;
using System.Collections.Generic;

namespace ScansApp.Models;

public sealed class Scan
{
    public Scan(string id, IReadOnlyList<string> planeAImages, IReadOnlyList<string> planeBImages)
    {
        Id = id;
        PlaneAImages = planeAImages ?? throw new ArgumentNullException(nameof(planeAImages));
        PlaneBImages = planeBImages ?? throw new ArgumentNullException(nameof(planeBImages));
    }

    public string Id { get; }

    public IReadOnlyList<string> PlaneAImages { get; }

    public IReadOnlyList<string> PlaneBImages { get; }
}
