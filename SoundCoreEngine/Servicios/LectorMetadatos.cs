namespace SoundCoreEngine.Servicios;

public sealed record MetadatosArchivo(string Titulo, string Artista, int Bpm, int DuracionSegundos, bool TieneBpm);

/// <summary>Lee título, artista, duración y BPM incrustados en archivos multimedia.</summary>
public static class LectorMetadatos
{
    public static MetadatosArchivo Leer(string ruta)
    {
        var tituloPredeterminado = Path.GetFileNameWithoutExtension(ruta);
        try
        {
            using var archivo = TagLib.File.Create(ruta);
            var titulo = string.IsNullOrWhiteSpace(archivo.Tag.Title) ? tituloPredeterminado : archivo.Tag.Title.Trim();
            var artista = string.IsNullOrWhiteSpace(archivo.Tag.FirstPerformer) ? "Artista desconocido" : archivo.Tag.FirstPerformer.Trim();
            var bpm = (int)archivo.Tag.BeatsPerMinute;
            var segundos = Math.Max(0, (int)Math.Round(archivo.Properties.Duration.TotalSeconds));
            return new MetadatosArchivo(titulo, artista, bpm, segundos, bpm is >= 60 and <= 220);
        }
        catch
        {
            return new MetadatosArchivo(tituloPredeterminado, "Artista desconocido", 0, 0, false);
        }
    }
}
