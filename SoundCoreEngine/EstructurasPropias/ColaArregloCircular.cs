using System.Collections;

namespace SoundCoreEngine.EstructurasPropias;

/// <summary>Cola FIFO genérica implementada desde cero sobre un arreglo circular de capacidad fija.</summary>
public sealed class ColaArregloCircular<T> : IEnumerable<T>
{
    private readonly T[] _elementos;
    private int _frente;
    private int _final;

    public ColaArregloCircular(int capacidad)
    {
        if (capacidad <= 0) throw new ArgumentOutOfRangeException(nameof(capacidad));
        _elementos = new T[capacidad];
    }

    public int Capacidad => _elementos.Length;
    public int Cantidad { get; private set; }
    public int Frente => Cantidad == 0 ? -1 : _frente;
    public int Final => Cantidad == 0 ? -1 : (_final - 1 + Capacidad) % Capacidad;
    public bool EstaVacia => Cantidad == 0;
    public bool EstaLlena => Cantidad == Capacidad;

    public void Encolar(T valor)
    {
        if (EstaLlena) throw new InvalidOperationException("La cola está llena.");
        _elementos[_final] = valor;
        _final = (_final + 1) % Capacidad;
        Cantidad++;
    }

    public T Desencolar()
    {
        if (EstaVacia) throw new InvalidOperationException("La cola está vacía.");
        var valor = _elementos[_frente];
        _elementos[_frente] = default!;
        _frente = (_frente + 1) % Capacidad;
        Cantidad--;
        return valor;
    }

    public T Peek()
    {
        if (EstaVacia) throw new InvalidOperationException("La cola está vacía.");
        return _elementos[_frente];
    }

    public void Limpiar()
    {
        Array.Clear(_elementos);
        _frente = 0;
        _final = 0;
        Cantidad = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Cantidad; i++)
            yield return _elementos[(_frente + i) % Capacidad];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
