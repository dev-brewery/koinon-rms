using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Koinon.PrintBridge.Helpers;

/// <summary>
/// Helper class for sending raw data to printers via winspool.drv P/Invoke.
/// </summary>
[SupportedOSPlatform("windows")]
public static class RawPrinterHelper
{
    // P/Invoke declarations for Windows printer API
    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool OpenPrinter(string printerName, out nint printerHandle, nint printerDefaults);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool ClosePrinter(nint printerHandle);

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool StartDocPrinter(
        nint printerHandle,
        int level,
        [In] ref DocInfo docInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(nint printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(
        nint printerHandle,
        nint pBytes,
        int dwCount,
        out int dwWritten);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DocInfo
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDocName;

        [MarshalAs(UnmanagedType.LPStr)]
        public string? pOutputFile;

        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDatatype;
    }

    /// <summary>
    /// Sends a string of data to the specified printer.
    /// </summary>
    /// <param name="printerName">The name of the printer.</param>
    /// <param name="data">The data to send (e.g., ZPL commands).</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool SendStringToPrinter(string printerName, string data)
    {
        if (string.IsNullOrEmpty(printerName) || string.IsNullOrEmpty(data))
        {
            return false;
        }

        var dataBytes = System.Text.Encoding.ASCII.GetBytes(data);
        return SendBytesToPrinter(printerName, dataBytes);
    }

    /// <summary>
    /// Sends raw bytes to the specified printer.
    /// </summary>
    /// <param name="printerName">The name of the printer.</param>
    /// <param name="data">The raw bytes to send.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool SendBytesToPrinter(string printerName, byte[] data)
    {
        if (string.IsNullOrEmpty(printerName) || data == null || data.Length == 0)
        {
            return false;
        }

        nint printerHandle = nint.Zero;
        var docInfo = new DocInfo
        {
            pDocName = "Koinon PrintBridge",
            pOutputFile = null,
            pDatatype = "RAW"
        };

        try
        {
            // Open printer
            if (!OpenPrinter(printerName, out printerHandle, nint.Zero))
            {
                return false;
            }

            // Start document
            if (!StartDocPrinter(printerHandle, 1, ref docInfo))
            {
                return false;
            }

            // Allocate unmanaged memory for data
            var unmanagedBytes = Marshal.AllocHGlobal(data.Length);
            try
            {
                // Copy data to unmanaged memory
                Marshal.Copy(data, 0, unmanagedBytes, data.Length);

                // Write data to printer
                if (!WritePrinter(printerHandle, unmanagedBytes, data.Length, out _))
                {
                    EndDocPrinter(printerHandle);
                    return false;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedBytes);
            }

            // End document
            return EndDocPrinter(printerHandle);
        }
        finally
        {
            // Close printer
            if (printerHandle != nint.Zero)
            {
                ClosePrinter(printerHandle);
            }
        }
    }
}
