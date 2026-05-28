using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Threading.Tasks;

namespace OceanStock.Services;

public class ScannerManager : IDisposable
{
    private SerialPort? mySerialPort;
    private string? portDetected;
    public QueueBuffer SerialBuffer = new();

    public void OpenPort()
    {
        if (mySerialPort != null)
        {
            try
            {
                if (mySerialPort.IsOpen) mySerialPort.Close();
                mySerialPort.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur fermeture port: " + ex.Message);
            }
            finally
            {
                mySerialPort = null;
            }
        }
        else
        {
            if (OperatingSystem.IsWindows())
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string id = queryObj["PNPDeviceID"]?.ToString() ?? "";
                    string nom = queryObj["Name"]?.ToString() ?? "";

                    if (id.Contains("PID_A4A7"))
                    {
                        int debut = nom.LastIndexOf("COM");
                        int fin = nom.LastIndexOf(")");

                        if (debut != -1 && fin != -1)
                        {
                            portDetected = nom.Substring(debut, fin - debut);
                            break;
                        }
                    }
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                string byId = "/dev/serial/by-id";

                if (Directory.Exists(byId))
                {
                    foreach (var device in Directory.GetFiles(byId))
                    {
                        if (device.Contains("A4A7", StringComparison.OrdinalIgnoreCase))
                        {
                            portDetected = Path.GetFullPath(device);
                            break;
                        }
                    }
                }
            }

            if (portDetected != null)
            {
                mySerialPort = new SerialPort
                {
                    PortName = portDetected,
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 10000,
                    WriteTimeout = 10000
                };

                mySerialPort.DataReceived += DataHandler;

                try
                {
                    mySerialPort.Open();
                }
                catch
                {
                    // Port détecté mais inaccessible (ex : déjà utilisé)
                }
            }
        }
    }

    //verif pour ne pas bloquer le thread UI
    public async Task OpenPortAsynk()
    {
        await Task.Run(() => OpenPort());
    }

    public void ClosePort()
    {
        if (mySerialPort != null && mySerialPort.IsOpen)
        {
            try
            {
                mySerialPort.Close();
                mySerialPort.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur fermeture port: " + ex.Message);
            }
            finally
            {
                mySerialPort = null;
            }
        }
    }

    private void DataHandler(object sender, EventArgs arg)
    {
        SerialPort sp = (SerialPort)sender;
        SerialBuffer.Enqueue(sp.ReadExisting());
    }

    public void SimulateScan(string barcode)
    {
        SerialBuffer.Enqueue(barcode + "\r\n");
    }

    public void Dispose() => ClosePort();

    public sealed class QueueBuffer : Queue
    {
        public event EventHandler? Changed;

        public override void Enqueue(object? obj)
        {
            base.Enqueue(obj);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
