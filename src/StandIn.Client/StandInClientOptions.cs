using System;

namespace StandIn.Client;

public sealed class StandInClientOptions
{
    /// <summary>Adresse der API, auf der StandIn laeuft, z. B. "https://localhost:5001/". Pflicht.</summary>
    public Uri? ServerAddress { get; set; }

    /// <summary>Root-Pfad von StandIn auf dem Server, Standard "/stuntman".</summary>
    public string RootPath { get; set; } = "/stuntman";

    /// <summary>Benutzer, mit dem sich der Client beim ersten Zugriff anmeldet. Ohne Angabe bleibt er anonym.</summary>
    public string? UserId { get; set; }
}
