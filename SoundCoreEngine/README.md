# SoundCore Engine — Cola con Arreglo

Aplicación de escritorio en C# / Windows Forms / .NET 8 que simula una cola de reproducción para DJ y sirve para explicar una **cola FIFO implementada sobre arreglos**.

## Objetivo

El proyecto adapta el trabajo original de listas enlazadas a estructuras de tipo **cola**. La estructura principal es una cola circular genérica creada desde cero con un arreglo `T[]`, sin `List<T>` ni nodos enlazados.

La aplicación permite comparar tres implementaciones de cola:

- `ColaArregloCircular<T>`: cola propia sobre arreglo circular. Es la implementación principal.
- `ColaArregloLineal<T>`: cola propia sobre arreglo que desplaza elementos al desencolar.
- `.NET Queue<T>`: cola nativa de .NET, usada como referencia.

## Cómo funciona la cola circular

La cola mantiene cuatro conceptos:

- `T[] elementos`: arreglo que almacena los datos.
- `frente`: índice donde está el siguiente elemento que saldrá.
- `final`: índice donde se insertará el siguiente elemento.
- `cantidad`: número de elementos actualmente almacenados.

Al encolar, el elemento se coloca en `final` y el índice avanza. Al llegar al último espacio, vuelve a `0` mediante módulo:

```csharp
_final = (_final + 1) % Capacidad;
```

Al desencolar, se toma el elemento de `frente` y también se avanza circularmente:

```csharp
_frente = (_frente + 1) % Capacidad;
```

Esto permite reutilizar posiciones liberadas sin desplazar todos los elementos.

## Operaciones principales

- `Encolar(T)`: agrega al final. O(1).
- `Desencolar()`: retira del frente. O(1).
- `Peek()`: consulta el frente sin eliminarlo. O(1).
- `EstaVacia`: indica si no hay elementos. O(1).
- `EstaLlena`: indica si se alcanzó la capacidad. O(1).
- `Limpiar()`: vacía el arreglo. O(n) por la limpieza de referencias.

## Ejemplo FIFO

Si entran:

`A → B → C → D`

la salida obligatoria es:

`A → B → C → D`

La primera pista en entrar es la primera en salir.

## Interfaz

1. Selecciona un archivo.
2. Elige el modo de cola.
3. Pulsa **Encolar**.
4. Usa **Peek** para consultar el primer elemento sin sacarlo.
5. Usa **Desencolar y reproducir** para retirar el frente y enviarlo a VLC.
6. Observa `frente`, `final`, `cantidad` e índice de cada pista en la tabla.
7. Usa **Vaciar cola** para reiniciar la estructura.

La capacidad visual de las colas propias es 12 elementos. `.NET Queue<T>` administra su almacenamiento internamente.

## Benchmark

La prueba carga `n` elementos y después desencola los `n` elementos. Se miden los tiempos con `Stopwatch`.

- Cola circular propia: encolar O(1), desencolar O(1).
- `.NET Queue<T>`: encolar O(1) amortizado, desencolar O(1).
- Cola lineal propia: desencolar O(n), porque desplaza los elementos restantes.

Los milisegundos son experimentales y dependen del equipo. Lo importante para la explicación es la diferencia de complejidad.

## Organización

```text
SoundCoreEngine/
├── EstructurasPropias/
│   ├── ColaArregloCircular.cs
│   └── ColaArregloLineal.cs
├── Modelos/
│   └── Pista.cs
├── Servicios/
│   ├── ReproductorVlc.cs
│   └── LectorMetadatos.cs
├── MainForm.cs
├── Program.cs
└── SoundCoreEngine.csproj
```

## Tecnologías

- C#
- .NET 8
- Windows Forms
- LibVLCSharp / LibVLC
- Arreglos genéricos `T[]`
- `Queue<T>` para comparación
- `Stopwatch` para benchmark

## Ejecución

1. Abrir `SoundCoreEngine.sln` en Visual Studio 2022.
2. Restaurar paquetes NuGet.
3. Compilar con Ctrl + Shift + B.
4. Ejecutar con F5.

## Nota sobre el entorno

En este paquete no se incluyen resultados de benchmark fijos. Deben medirse en el equipo donde se haga la demostración. Tampoco se afirma que los tiempos sean idénticos entre computadoras.
