using System.Collections;

namespace SoundCoreEngine.EstructurasPropias;

/// <summary>Cola FIFO genérica sobre un arreglo. Al desencolar desplaza los elementos restantes.</summary>
public sealed class ColaArregloLineal<T> : IEnumerable<T>
{
    private readonly T[] _elementos;

    public ColaArregloLineal(int capacidad)
    {
        if (capacidad <= 0) throw new ArgumentOutOfRangeException(nameof(capacidad));
        _elementos = new T[capacidad];
    }

    public int Capacidad => _elementos.Length;
    public int Cantidad { get; private set; }
    public int Frente => Cantidad == 0 ? -1 : 0;
    public int Final => Cantidad == 0 ? -1 : Cantidad - 1;
    public bool EstaVacia => Cantidad == 0;
    public bool EstaLlena => Cantidad == Capacidad;

    public void Encolar(T valor)
    {
        if (EstaLlena) throw new InvalidOperationException("La cola está llena.");
        _elementos[Cantidad++] = valor;
    }

    public T Desencolar()
    {
        if (EstaVacia) throw new InvalidOperationException("La cola está vacía.");
        var valor = _elementos[0];
        for (var i = 1; i < Cantidad; i++) _elementos[i - 1] = _elementos[i];
        _elementos[--Cantidad] = default!;
        return valor;
    }

    public T Peek()
    {
        if (EstaVacia) throw new InvalidOperationException("La cola está vacía.");
        return _elementos[0];
    }

    public void Limpiar() { Array.Clear(_elementos, 0, _elementos.Length); Cantidad = 0; }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Cantidad; i++) yield return _elementos[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
