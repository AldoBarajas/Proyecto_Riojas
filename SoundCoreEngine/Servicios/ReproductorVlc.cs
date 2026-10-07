using LibVLCSharp.Shared;
using SoundCoreEngine.Modelos;

namespace SoundCoreEngine.Servicios;

/// <summary>Adaptador de reproducción basado en LibVLC, distribuido por NuGet.</summary>
public sealed class ReproductorVlc : IDisposable
{
    private readonly LibVLC _vlc;
    private readonly MediaPlayer _player;
    private Media? _media;

    public event EventHandler? Termino;
    public event EventHandler<string>? Error;
    public bool EstaReproduciendo => _player.IsPlaying;
    public int Volumen { get => _player.Volume; set => _player.Volume = Math.Clamp(value, 0, 100); }

    public ReproductorVlc()
    {
        Core.Initialize();
        _vlc = new LibVLC("--no-video", "--quiet");
        _player = new MediaPlayer(_vlc);
        _player.EndReached += (_, _) => Termino?.Invoke(this, EventArgs.Empty);
        _player.EncounteredError += (_, _) => Error?.Invoke(this, "VLC no pudo reproducir este archivo.");
    }

    public void Reproducir(Pista pista)
    {
        if (!File.Exists(pista.RutaArchivo)) throw new FileNotFoundException("El archivo ya no existe.", pista.RutaArchivo);
        _player.Stop();
        _media?.Dispose();
        _media = new Media(_vlc, new Uri(pista.RutaArchivo));
        _player.Play(_media);
    }

    public void PausarOReanudar()
    {
        if (_player.IsPlaying) _player.Pause(); else _player.Play();
    }

    public void Detener() => _player.Stop();
    public void Dispose() { _media?.Dispose(); _player.Dispose(); _vlc.Dispose(); }
}
