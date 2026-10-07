using System.Diagnostics;
using SoundCoreEngine.EstructurasPropias;
using SoundCoreEngine.Modelos;
using SoundCoreEngine.Servicios;

namespace SoundCoreEngine;

public sealed class MainForm : Form
{
    private const int CapacidadVisual = 12;
    private readonly ColaArregloCircular<Pista> _colaCircular = new(CapacidadVisual);
    private readonly ColaArregloLineal<Pista> _colaLineal = new(CapacidadVisual);
    private readonly Queue<Pista> _colaDotNet = new();
    private readonly ReproductorVlc _reproductor = new();
    private readonly TextBox _txtTitulo = new() { PlaceholderText = "Título" };
    private readonly TextBox _txtArtista = new() { PlaceholderText = "Artista" };
    private readonly NumericUpDown _numBpm = new() { Minimum = 0, Maximum = 220, Value = 0, Width = 70 };
    private readonly NumericUpDown _numDuracion = new() { Minimum = 0, Maximum = 36000, Value = 180, Width = 80 };
    private readonly RadioButton _rbCircular = new() { Text = "Cola circular propia (arreglo)", Checked = true, AutoSize = true };
    private readonly RadioButton _rbLineal = new() { Text = "Cola lineal propia (arreglo)", AutoSize = true };
    private readonly RadioButton _rbDotNet = new() { Text = ".NET Queue<T>", AutoSize = true };
    private readonly DataGridView _grid = new();
    private readonly Label _lblArchivo = new() { Text = "Ningún archivo seleccionado", AutoEllipsis = true, Dock = DockStyle.Fill };
    private readonly Label _lblMetadatos = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label _lblSonando = new() { Text = "▶ En espera", AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.DarkCyan };
    private readonly Label _lblCronometro = new() { Text = "⏱ 00:00 / 00:00", AutoSize = true, Font = new Font("Consolas", 10, FontStyle.Bold), ForeColor = Color.MidnightBlue };
    private readonly Label _lblEstadisticas = new() { AutoSize = true };
    private readonly Label _lblEstadoArreglo = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly TextBox _txtResultados = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 30, 45), ForeColor = Color.White, Font = new Font("Consolas", 9) };
    private readonly NumericUpDown _numPrueba = new() { Minimum = 1000, Maximum = 10000, Increment = 1000, Value = 5000, Width = 95 };
    private string? _archivoSeleccionado;
    private Pista? _pistaSonando;
    private int _siguienteId = 1;
    private readonly Stopwatch _cronometroReproduccion = new();
    private readonly System.Windows.Forms.Timer _temporizadorReproduccion = new() { Interval = 250 };
    private static readonly HashSet<string> ExtensionesMultimedia = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".flac", ".ogg", ".opus", ".m4a", ".aac", ".wma", ".aiff", ".ape",
        ".mp4", ".mkv", ".avi", ".wmv", ".webm", ".mpeg", ".mpg"
    };

    public MainForm()
    {
        Text = "SoundCore Engine — Cola con Arreglo & VLC Player";
        MinimumSize = new Size(1050, 700);
        Size = new Size(1200, 780);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(241, 245, 249);
        ConstruirInterfaz();
        _rbCircular.CheckedChanged += (_, _) => RefrescarVista();
        _rbLineal.CheckedChanged += (_, _) => RefrescarVista();
        _rbDotNet.CheckedChanged += (_, _) => RefrescarVista();
        _reproductor.Termino += (_, _) => BeginInvoke(AvanzarYReproducir);
        _reproductor.Error += (_, mensaje) => BeginInvoke(() => MessageBox.Show(mensaje, "Formato no compatible", MessageBoxButtons.OK, MessageBoxIcon.Warning));
        _temporizadorReproduccion.Tick += (_, _) => ActualizarCronometro();
        FormClosed += (_, _) => { _temporizadorReproduccion.Dispose(); _reproductor.Dispose(); };
        RefrescarVista();
    }

    private void ConstruirInterfaz()
    {
        var raiz = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 3, ColumnCount = 1 };
        raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 60));
        raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        raiz.Controls.Add(CrearRegistro(), 0, 0);
        raiz.Controls.Add(CrearZonaCola(), 0, 1);
        raiz.Controls.Add(CrearBenchmark(), 0, 2);
        Controls.Add(raiz);
    }

    private Control CrearRegistro()
    {
        var caja = Caja("REGISTRO DE ARCHIVO / PISTA");
        caja.MinimumSize = new Size(0, 170);
        var fila = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(8) };
        var btnArchivo = Boton("📄 Elegir un archivo", (_, _) => ElegirArchivo());
        var btnVarios = Boton("📚 Agregar varios archivos", (_, _) => ElegirVariosArchivos());
        var btnCarpeta = Boton("📂 Agregar carpeta", (_, _) => ElegirCarpeta());
        fila.Controls.AddRange([btnArchivo, btnVarios, btnCarpeta, _lblArchivo, Etiqueta("Título:"), _txtTitulo, Etiqueta("Artista:"), _txtArtista,
            Etiqueta("BPM:"), _numBpm, Etiqueta("Duración (s):"), _numDuracion]);
        fila.Controls.Add(_lblMetadatos);
        fila.Controls.Add(Etiqueta("Modo de cola:"));
        fila.Controls.AddRange([_rbCircular, _rbLineal, _rbDotNet]);
        caja.Controls.Add(fila);
        return caja;
    }

    private Control CrearZonaCola()
    {
        var division = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 255, Panel1MinSize = 220 };
        var acciones = Caja("ACCIONES DE COLA FIFO");
        var botones = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12) };
        botones.Controls.AddRange([
            Boton("+ Encolar", (_, _) => Encolar()),
            Boton("👁 Ver frente (Peek)", (_, _) => VerFrente()),
            Boton("▶ Desencolar y reproducir", (_, _) => AvanzarYReproducir()),
            Boton("⏸ Pausar / reanudar", (_, _) => PausarOReanudar()),
            Boton("🗑 Vaciar cola", (_, _) => VaciarCola())]);
        acciones.Controls.Add(botones);
        division.Panel1.Controls.Add(acciones);

        var cola = Caja("PLAYLIST VISUAL / COLA FIFO EN VIVO");
        var superior = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        superior.Controls.Add(_lblSonando);
        superior.Controls.Add(_lblCronometro);
        superior.Controls.Add(_lblEstadoArreglo);
        cola.Controls.Add(CrearPieCola());
        cola.Controls.Add(_grid);
        cola.Controls.Add(superior);
        ConfigurarGrid();
        division.Panel2.Controls.Add(cola);
        return division;
    }

    private Control CrearPieCola()
    {
        var pie = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(8) };
        pie.Controls.Add(_lblEstadisticas);
        return pie;
    }

    private Control CrearBenchmark()
    {
        var caja = Caja("BENCHMARK Y TELEMETRÍA");
        var cabecera = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8) };
        cabecera.Controls.AddRange([Etiqueta("Operaciones para prueba:"), _numPrueba, Boton("🚀 Iniciar prueba", (_, _) => EjecutarBenchmark())]);
        caja.Controls.Add(_txtResultados);
        caja.Controls.Add(cabecera);
        return caja;
    }

    private void ConfigurarGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.ReadOnly = true;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.ColumnCount = 7;
        string[] nombres = ["Pos.", "Índice", "ID", "Título / artista", "BPM", "Duración", "Formato"];
        for (var i = 0; i < nombres.Length; i++) _grid.Columns[i].Name = nombres[i];
    }

    private void ElegirArchivo()
    {
        using var dialogo = new OpenFileDialog { Filter = "Todos los archivos (*.*)|*.*", Title = "Selecciona un archivo para VLC" };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        _archivoSeleccionado = dialogo.FileName;
        _lblArchivo.Text = _archivoSeleccionado;
        _txtTitulo.Text = Path.GetFileNameWithoutExtension(_archivoSeleccionado);
        if (string.IsNullOrWhiteSpace(_txtArtista.Text)) _txtArtista.Text = "Artista desconocido";
    }

    private void ElegirVariosArchivos()
    {
        using var dialogo = new OpenFileDialog { Filter = "Archivos multimedia (*.*)|*.*", Multiselect = true, Title = "Selecciona varios archivos" };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        foreach (var archivo in dialogo.FileNames) EncolarArchivo(archivo);
    }

    private void ElegirCarpeta()
    {
        using var dialogo = new FolderBrowserDialog { Description = "Selecciona una carpeta con archivos multimedia" };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        foreach (var archivo in Directory.EnumerateFiles(dialogo.SelectedPath))
            if (ExtensionesMultimedia.Contains(Path.GetExtension(archivo))) EncolarArchivo(archivo);
    }

    private void EncolarArchivo(string archivo)
    {
        _archivoSeleccionado = archivo;
        _lblArchivo.Text = archivo;
        _txtTitulo.Text = Path.GetFileNameWithoutExtension(archivo);
        if (string.IsNullOrWhiteSpace(_txtArtista.Text)) _txtArtista.Text = "Artista desconocido";
        try { Encolar(); } catch (InvalidOperationException ex) { Aviso(ex.Message); }
    }

    private Pista CrearPista()
    {
        if (string.IsNullOrWhiteSpace(_archivoSeleccionado)) throw new InvalidOperationException("Primero selecciona un archivo.");
        return new Pista(_siguienteId++,
            string.IsNullOrWhiteSpace(_txtTitulo.Text) ? Path.GetFileNameWithoutExtension(_archivoSeleccionado) : _txtTitulo.Text.Trim(),
            string.IsNullOrWhiteSpace(_txtArtista.Text) ? "Artista desconocido" : _txtArtista.Text.Trim(),
            (int)_numBpm.Value, (int)_numDuracion.Value, _archivoSeleccionado);
    }

    private void Encolar()
    {
        try
        {
            var pista = CrearPista();
            if (_rbCircular.Checked) _colaCircular.Encolar(pista);
            else if (_rbLineal.Checked) _colaLineal.Encolar(pista);
            else _colaDotNet.Enqueue(pista);
            RefrescarVista();
        }
        catch (InvalidOperationException ex) { Aviso(ex.Message); }
    }

    private void VerFrente()
    {
        try
        {
            var pista = Frente();
            Aviso($"FRONT DE LA COLA\n\n{pista.Titulo} — {pista.Artista}\nBPM: {pista.Bpm}\n\nPeek() consulta el primer elemento sin eliminarlo.");
        }
        catch (InvalidOperationException) { Aviso("La cola está vacía."); }
    }

    private Pista Frente() => _rbCircular.Checked ? _colaCircular.Peek() : _rbLineal.Checked ? _colaLineal.Peek() : _colaDotNet.Peek();

    private void AvanzarYReproducir()
    {
        try
        {
            _pistaSonando = SacarFrente();
            _reproductor.Reproducir(_pistaSonando);
            _cronometroReproduccion.Restart();
            _lblSonando.Text = $"▶ Sonando: {_pistaSonando.Titulo} — {_pistaSonando.Artista} [{_pistaSonando.Formato}]";
            RefrescarVista();
        }
        catch (InvalidOperationException) { Aviso("No hay pistas pendientes en la cola seleccionada."); }
        catch (FileNotFoundException ex) { Aviso(ex.Message); RefrescarVista(); }
    }

    private Pista SacarFrente() => _rbCircular.Checked ? _colaCircular.Desencolar() : _rbLineal.Checked ? _colaLineal.Desencolar() : _colaDotNet.Dequeue();

    private void VaciarCola()
    {
        if (_rbCircular.Checked) _colaCircular.Limpiar();
        else if (_rbLineal.Checked) _colaLineal.Limpiar();
        else _colaDotNet.Clear();
        RefrescarVista();
    }

    private IEnumerable<Pista> ColaActiva() => _rbCircular.Checked ? _colaCircular : _rbLineal.Checked ? _colaLineal : _colaDotNet;

    private void RefrescarVista()
    {
        _grid.Rows.Clear();
        var totalSegundos = 0;
        var posicion = 1;
        foreach (var p in ColaActiva())
        {
            var indice = posicion - 1;
            if (_rbCircular.Checked) indice = (_colaCircular.Frente + posicion - 1) % CapacidadVisual;
            _grid.Rows.Add(posicion++, indice, p.Id, $"{p.Titulo} — {p.Artista}", p.Bpm, FormatearTiempo(p.DuracionSegundos), p.Formato);
            totalSegundos += p.DuracionSegundos;
        }
        _lblEstadisticas.Text = $"Total en cola: {posicion - 1}  |  Duración estimada: {FormatearTiempo(totalSegundos)}";
        if (_rbCircular.Checked)
            _lblEstadoArreglo.Text = $"Arreglo: capacidad {CapacidadVisual} | frente={_colaCircular.Frente} | final={_colaCircular.Final} | cantidad={_colaCircular.Cantidad}";
        else if (_rbLineal.Checked)
            _lblEstadoArreglo.Text = $"Arreglo: capacidad {CapacidadVisual} | frente={_colaLineal.Frente} | final={_colaLineal.Final} | cantidad={_colaLineal.Cantidad}";
        else
            _lblEstadoArreglo.Text = $"Queue<T>: cantidad={_colaDotNet.Count} | FIFO administrado por .NET";
    }

    private void EjecutarBenchmark()
    {
        var n = (int)_numPrueba.Value;
        var sw = new Stopwatch();

        var circular = new ColaArregloCircular<int>(n);
        sw.Start();
        for (var i = 0; i < n; i++) circular.Encolar(i);
        for (var i = 0; i < n; i++) circular.Desencolar();
        sw.Stop();
        var tCircular = sw.ElapsedTicks;

        var dotNet = new Queue<int>(n);
        sw.Restart();
        for (var i = 0; i < n; i++) dotNet.Enqueue(i);
        for (var i = 0; i < n; i++) dotNet.Dequeue();
        sw.Stop();
        var tDotNet = sw.ElapsedTicks;

        var lineal = new ColaArregloLineal<int>(n);
        for (var i = 0; i < n; i++) lineal.Encolar(i);
        sw.Restart();
        for (var i = 0; i < n; i++) lineal.Desencolar();
        sw.Stop();
        var tLineal = sw.ElapsedTicks;

        var frecuencia = Stopwatch.Frequency;
        double Ms(long ticks) => ticks * 1000.0 / frecuencia;
        _txtResultados.Text = $"=== {n:N0} ENCOLADOS + {n:N0} DESENCOLADOS ==={Environment.NewLine}" +
            $"Cola circular propia: {Ms(tCircular),9:F3} ms | Encolar O(1), Desencolar O(1){Environment.NewLine}" +
            $".NET Queue<T>:        {Ms(tDotNet),9:F3} ms | Encolar O(1) amortizado, Desencolar O(1){Environment.NewLine}" +
            $"Cola lineal propia:   {Ms(tLineal),9:F3} ms | Desencolar O(n) por desplazamiento{Environment.NewLine}{Environment.NewLine}" +
            "Conclusión: la cola circular reutiliza posiciones del arreglo mediante índices y evita desplazar elementos.\n" +
            "Los tiempos son experimentales y dependen del equipo; la complejidad explica el comportamiento esperado.";
    }

    private void PausarOReanudar()
    {
        _reproductor.PausarOReanudar();
        if (_reproductor.EstaReproduciendo) _cronometroReproduccion.Start();
        else _cronometroReproduccion.Stop();
    }

    private void ActualizarCronometro()
    {
        if (_pistaSonando is null) return;
        var transcurrido = _cronometroReproduccion.Elapsed.TotalSeconds;
        _lblCronometro.Text = $"⏱ {FormatearTiempo((int)transcurrido)} / {FormatearTiempo(_pistaSonando.DuracionSegundos)}";
    }

    private static GroupBox Caja(string texto) => new() { Text = texto, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9, FontStyle.Bold), Padding = new Padding(8) };
    private static Label Etiqueta(string texto) => new() { Text = texto, AutoSize = true, Padding = new Padding(5, 6, 2, 2) };
    private static Button Boton(string texto, EventHandler evento)
    {
        var boton = new Button { Text = texto, AutoSize = true, MinimumSize = new Size(190, 34), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Margin = new Padding(4) };
        boton.Click += evento;
        return boton;
    }
    private static string FormatearTiempo(int segundos) => TimeSpan.FromSeconds(segundos).ToString(segundos >= 3600 ? @"h\:mm\:ss" : @"mm\:ss");
    private void Aviso(string mensaje) => MessageBox.Show(mensaje, "SoundCore Engine", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
