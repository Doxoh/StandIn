using System;

namespace StandIn.Client;

public sealed class StandInClientOptions
{
    /// <summary>Address of the API that runs StandIn, e.g. "https://localhost:5001/". Required.</summary>
    public Uri? ServerAddress { get; set; }

    /// <summary>Root path of StandIn on the server, default "/stuntman".</summary>
    public string RootPath { get; set; } = "/stuntman";

    /// <summary>User the client signs in as on first access. Without a value it stays anonymous.</summary>
    public string? UserId { get; set; }
}
