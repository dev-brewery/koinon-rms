using System.Drawing;
using System.Windows.Forms;
using Koinon.PrintBridge;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Koinon",
            "PrintBridge",
            "logs",
            "printbridge-.log"
        ),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7
    )
    .CreateLogger();

try
{
    Log.Information("Koinon Print Bridge starting...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Kestrel to listen on localhost only
    // Port can be configured via appsettings.json or environment variable PRINT_BRIDGE_PORT
    var port = builder.Configuration.GetValue<int>("PrintBridge:Port", 9632);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(port);
    });

    // Add services
    builder.Services.AddSingleton<PrinterDiscoveryService>();
    builder.Services.AddSingleton<ZplPrintService>();
    builder.Services.AddSingleton<WindowsPrintService>();
    builder.Services.AddControllers();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowKiosk", policy =>
        {
            // SECURITY: Only allow requests from localhost web app (Vite dev server)
            // NEVER allow production origins - this is a localhost-only print bridge
            policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // Add Serilog
    builder.Host.UseSerilog();

    var app = builder.Build();

    app.UseCors("AllowKiosk");
    app.MapControllers();

    // Health check endpoint
    app.MapGet("/health", () => new
    {
        status = "healthy",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    });

    // Initialize services
    var printerDiscovery = app.Services.GetRequiredService<PrinterDiscoveryService>();
    await printerDiscovery.InitializeAsync();

    // Start the system tray application in the background
    var trayIcon = new SystemTrayIcon(app.Services);
    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);

    // Start web server in background thread
    var webServerTask = Task.Run(async () => await app.RunAsync());

    Log.Information("Koinon Print Bridge started successfully on port 9632");

    // Run Windows Forms message loop (keeps tray icon alive)
    Application.Run(new HiddenForm(trayIcon));

    // Wait for web server to complete
    await webServerTask;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Hidden form to keep the application running
internal class HiddenForm : Form
{
    public HiddenForm(SystemTrayIcon trayIcon)
    {
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;
        Opacity = 0;
        FormBorderStyle = FormBorderStyle.None;

        // Prevent closing via form events - only allow exit via tray icon
        FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
        };
    }
}

// System tray icon implementation
internal class SystemTrayIcon
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IServiceProvider _services;

    public SystemTrayIcon(IServiceProvider services)
    {
        _services = services;

        _notifyIcon = new NotifyIcon
        {
            Icon = CreateIcon(),
            Visible = true,
            Text = "Koinon Print Bridge"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Test Print", null, OnTestPrint);
        contextMenu.Items.Add("View Printers", null, OnViewPrinters);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, OnExit);

        _notifyIcon.ContextMenuStrip = contextMenu;

        // Double-click to show printers
        _notifyIcon.DoubleClick += OnViewPrinters;

        _notifyIcon.BalloonTipTitle = "Koinon Print Bridge";
        _notifyIcon.BalloonTipText = "Print bridge is running and ready to print labels";
        _notifyIcon.ShowBalloonTip(3000);
    }

    private static Icon CreateIcon()
    {
        // Create a simple icon with a "P" for printer
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
            g.FillEllipse(Brushes.DodgerBlue, 2, 2, 28, 28);
            g.DrawString("P", new Font("Arial", 16, FontStyle.Bold), Brushes.White, 8, 4);
        }

        return Icon.FromHandle(bitmap.GetHicon());
    }

    private async void OnTestPrint(object? sender, EventArgs e)
    {
        try
        {
            var zplPrintService = _services.GetRequiredService<ZplPrintService>();
            var windowsPrintService = _services.GetRequiredService<WindowsPrintService>();
            var printerDiscovery = _services.GetRequiredService<PrinterDiscoveryService>();

            var printers = await printerDiscovery.GetAvailablePrintersAsync();
            var labelPrinter = printers.FirstOrDefault(p => p.IsZebraPrinter || p.IsDymoPrinter);

            if (labelPrinter == null)
            {
                MessageBox.Show(
                    "No label printer found. Please ensure a Zebra or Dymo label printer is installed and turned on.",
                    "No Printer Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // Print using appropriate method based on printer type
            if (labelPrinter.IsZebraPrinter)
            {
                // Generate test ZPL label
                var testZpl = @"^XA
^FO50,50^A0N,50,50^FDTest Label^FS
^FO50,120^A0N,30,30^FD" + DateTime.Now.ToString("g") + @"^FS
^FO50,160^A0N,25,25^FDKoinon Print Bridge^FS
^XZ";

                await zplPrintService.PrintZplAsync(labelPrinter.Name, testZpl);
            }
            else if (labelPrinter.IsDymoPrinter)
            {
                // Print simple text label for Dymo
                await windowsPrintService.PrintTextLabelAsync(
                    labelPrinter.Name,
                    $"Test Label\n{DateTime.Now:g}\nKoinon Print Bridge",
                    "default");
            }

            _notifyIcon.ShowBalloonTip(
                3000,
                "Test Print Sent",
                $"Test label sent to {labelPrinter.Name}",
                ToolTipIcon.Info
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Test print failed");
            MessageBox.Show(
                $"Test print failed: {ex.Message}",
                "Print Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private async void OnViewPrinters(object? sender, EventArgs e)
    {
        try
        {
            var printerDiscovery = _services.GetRequiredService<PrinterDiscoveryService>();
            var printers = await printerDiscovery.GetAvailablePrintersAsync();

            var message = "Available Printers:\n\n";
            if (printers.Count == 0)
            {
                message += "No printers found.";
            }
            else
            {
                foreach (var printer in printers)
                {
                    message += $"• {printer.Name}\n";
                    message += $"  Type: {printer.PrinterType}\n";
                    message += $"  Status: {printer.Status}\n";
                    message += $"  Supports ZPL: {(printer.SupportsZpl ? "Yes" : "No")}\n";
                    message += $"  Supports Image: {(printer.SupportsImage ? "Yes" : "No")}\n\n";
                }
            }

            MessageBox.Show(message, "Koinon Print Bridge - Printers", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve printers");
            MessageBox.Show(
                $"Failed to retrieve printers: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        Application.Exit();
        Environment.Exit(0);
    }
}
