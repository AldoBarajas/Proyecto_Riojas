namespace SoundCoreEngine.Modelos;

/// <summary>Representa cualquier archivo multimedia que el motor VLC pueda abrir.</summary>
public sealed record Pista(
    int Id,
    string Titulo,
    string Artista,
    int Bpm,
    int DuracionSegundos,
    string RutaArchivo)
{
    public string Formato => Path.GetExtension(RutaArchivo).TrimStart('.').ToUpperInvariant() switch
    {
        "" => "ARCHIVO",
        var extension => extension
    };

    public override string ToString() => $"{Titulo} — {Artista} ({Formato})";
}
