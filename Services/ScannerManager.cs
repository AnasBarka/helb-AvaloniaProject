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
        // Si le port est déjà ouvert, inutile de recommencer
        if (mySerialPort?.IsOpen == true)
            return;

        // Nettoyage de l'éventuel port précédent (fermé ou en erreur)
        if (mySerialPort != null)
        {
            try { mySerialPort.Dispose(); }
            catch { }
            mySerialPort = null;
        }

        portDetected = null;

        if (OperatingSystem.IsWindows())
        {
            // Recherche du scanner via le PID USB dans le registre WMI
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                string pnpId = queryObj["PNPDeviceID"]?.ToString() ?? "";
                string nom   = queryObj["Name"]?.ToString() ?? "";

                if (pnpId.Contains("PID_A4A7", StringComparison.OrdinalIgnoreCase))
                {
                    int debut = nom.LastIndexOf("COM");
                    int fin   = nom.LastIndexOf(")");
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
            // Lecture du PID USB via sysfs (le dossier /dev/serial/by-id contient des symlinks
            // dont le nom ne reflète pas le PID — il faut lire idProduct dans sysfs)
            const string sysTty = "/sys/class/tty";
            if (Directory.Exists(sysTty))
            {
                foreach (var ttyDir in Directory.GetDirectories(sysTty))
                {
                    try
                    {
                        string deviceLink = Path.Combine(ttyDir, "device");
                        if (!Directory.Exists(deviceLink))
                            continue;

                        // Résolution du lien symbolique vers le répertoire de l'interface USB
                        var resolved = new DirectoryInfo(deviceLink).ResolveLinkTarget(returnFinalTarget: true);
                        if (resolved == null)
                            continue;

                        // idProduct se trouve dans le répertoire parent (device USB, pas l'interface)
                        string usbDevDir = Path.GetDirectoryName(resolved.FullName) ?? "";
                        string pidFile   = Path.Combine(usbDevDir, "idProduct");

                        if (File.Exists(pidFile))
                        {
                            string pid = File.ReadAllText(pidFile).Trim();
                            if (pid.Equals("a4a7", StringComparison.OrdinalIgnoreCase))
                            {
                                portDetected = "/dev/" + Path.GetFileName(ttyDir);
                                break;
                            }
                        }
                    }
                    catch { /* port inaccessible ou chemin inexistant */ }
                }
            }
        }

        if (portDetected != null)
        {
            mySerialPort = new SerialPort
            {
                PortName  = portDetected,
                BaudRate  = 9600,
                DataBits  = 8,
                Parity    = Parity.None,
                StopBits  = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout  = 10000,
                WriteTimeout = 10000
            };

            mySerialPort.DataReceived += DataHandler;

            try
            {
                mySerialPort.Open();
            }
            catch
            {
                // Port détecté mais inaccessible (ex : permissions, déjà utilisé par autre process)
                mySerialPort.Dispose();
                mySerialPort = null;
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
