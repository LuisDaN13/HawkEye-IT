using System;
using System.Management;
using System.Net;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

class Program
{
    static void Main()
    {
        // ===========================================================================
        // Variables para almacenar la información del equipo
        // Marca, Modelo y Número de Serie
        var marca = "";
        var modelo = "";
        var serialPC = "";

        // Procesador y Sistema Operativo
        var modeloProcesador = "";
        var mhz = "";
        var so = "";
        var buildNumber = "";

        // RAM
        double totalRam = 0;
        int slotsRam = 0;
        var ramDetalle = new List<string>();

        // Almacenamiento
        var discosModelo = new List<string>();
        var discosBus = new List<string>();
        var discosTipo = new List<string>();
        var discosCapacidadGB = new List<ulong>();
        double almacenamientoLibreGB = 0;
        double almacenamientoTotalGB = 0;
        double almacenamientoPorcentajeLibre = 0;

        // Red
        var redInterfaces = new List<string>();
        var redMACs = new List<string>();
        var redIPs = new List<string>();

        // Monitor
        var monitoresFabricante = new List<string>();
        var monitoresModelo = new List<string>();
        var monitoresSerial = new List<string>();
        var monitoresActivo = new List<bool>();

        bool tiene7 = false;
        bool tiene6 = false;
        bool tieneKonica = false;

        // ===========================================================================
        // Inicio del Programa
        Console.WriteLine("===== INFORMACIÓN DEL EQUIPO =====\n");

        // Usuario
        Console.WriteLine("=== Usuario ===");
        Console.WriteLine($"Usuario: {Environment.UserName}");
        Console.WriteLine($"Nombre del equipo: {Environment.MachineName}\n");

        // Marca y Modelo
        var cs = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
        foreach (var obj in cs.Get())
        {
            Console.WriteLine($"Marca: {obj["Manufacturer"]}");
            marca = obj["Manufacturer"].ToString();
            Console.WriteLine($"Modelo: {obj["Model"]}");
            modelo = obj["Model"].ToString();
        }

        // Número de Serie
        var bios = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
        foreach (var obj in bios.Get())
        {
            Console.WriteLine($"Número de Serie: {obj["SerialNumber"]}");
            serialPC = obj["SerialNumber"].ToString();
        }

        // Procesador
        var cpu = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
        foreach (var obj in cpu.Get())
        {
            Console.WriteLine("\n=== Procesador ===");
            Console.WriteLine($"Modelo: {obj["Name"]}");
            modeloProcesador = obj["Name"].ToString();
            Console.WriteLine($"Velocidad: {obj["MaxClockSpeed"]} MHz");
            mhz = obj["MaxClockSpeed"].ToString();
        }

        // Sistema Operativo
        var os = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
        foreach (var obj in os.Get())
        {
            Console.WriteLine("\n=== Sistema Operativo ===");
            Console.WriteLine($"OS: {obj["Caption"]} {obj["OSArchitecture"]}");
            so = obj["Caption"].ToString() + " " + obj["OSArchitecture"].ToString();
            Console.WriteLine($"Build: {obj["BuildNumber"]}");
            buildNumber = obj["BuildNumber"].ToString();
        }

        // RAM
        var ram = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
        int slots = 0;

        Console.WriteLine("\n=== RAM ===");

        foreach (var obj in ram.Get())
        {
            double capacity = Convert.ToDouble(obj["Capacity"]) / (1024 * 1024 * 1024);
            totalRam += capacity;
            slots++;

            Console.WriteLine($"Slot {slots}: {capacity:F2} GB - {obj["Speed"]} MHz");
        }

        Console.WriteLine($"Total RAM: {totalRam:F2} GB");
        Console.WriteLine($"Slots usados: {slots}");

        // Almacenamiento
        Console.WriteLine("\n=== Almacenamiento ===");

        var scope = new ManagementScope(@"\\.\ROOT\Microsoft\Windows\Storage");
        var query = new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk");
        var searcher = new ManagementObjectSearcher(scope, query);

        foreach (ManagementObject disk in searcher.Get())
        {
            string model = disk["Model"]?.ToString() ?? "Desconocido";
            Console.WriteLine($"Modelo: {model}");

            int busType = disk["BusType"] != null ? Convert.ToInt32(disk["BusType"]) : -1;
            string bus = busType switch
            {
                17 => "NVMe",
                11 => "SATA",
                7 => "USB",
                _ => "Otro"
            };
            Console.WriteLine($"Bus: {bus}");

            string tipo = "Desconocido";
            if (disk["MediaType"] != null)
            {
                int media = Convert.ToInt32(disk["MediaType"]);
                tipo = media switch
                {
                    3 => "HDD",
                    4 => "SSD",
                    5 => "SCM",
                    _ => "Desconocido"
                };
            }
            else
            {
                if (busType == 17) tipo = "NVMe SSD";
                else if (model.ToLower().Contains("ssd")) tipo = "SSD";
                else tipo = "HDD / Desconocido";
            }
            Console.WriteLine($"Tipo Detectado: {tipo}");

            if (disk["Size"] != null)
            {
                ulong size = (ulong)disk["Size"];

                double totalFreeGB = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Sum(d => d.TotalFreeSpace / (1024.0 * 1024 * 1024));

                double totalLogicalGB = DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Sum(d => d.TotalSize / (1024.0 * 1024 * 1024));

                Console.WriteLine($"Tamaño Físico: {size / (1024 * 1024 * 1024)} GB");
                Console.WriteLine($"Espacio Disponible: {totalFreeGB:F2} GB");
                Console.WriteLine($"% Libre: {(totalFreeGB / totalLogicalGB * 100):F2}%");
            }
        }

        // Red
        Console.WriteLine("\n=== RED ===");

        var interfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(i =>
                i.OperationalStatus == OperationalStatus.Up &&
                i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                i.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                i.GetPhysicalAddress().GetAddressBytes().Length > 0);

        foreach (var ni in interfaces)
        {
            Console.WriteLine($"\nInterfaz: {ni.Name}");

            // MAC
            string mac = string.Join(":", ni.GetPhysicalAddress()
                .GetAddressBytes()
                .Select(b => b.ToString("X2")));

            Console.WriteLine($"MAC: {mac}");

            // IPs
            var ipProps = ni.GetIPProperties();

            foreach (var addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address) &&
                    !addr.Address.ToString().StartsWith("169.254"))
                {
                    Console.WriteLine($"IP: {addr.Address}");
                }
            }
        }

        // Monitor
        Console.WriteLine("\n=== Monitor ===");
        var scopeMonitor = new ManagementScope(@"\\.\root\wmi");
        var queryMonitor = new ObjectQuery("SELECT * FROM WmiMonitorID");
        var searcherMonitor = new ManagementObjectSearcher(scopeMonitor, queryMonitor);

        foreach (ManagementObject m in searcherMonitor.Get())
        {
            bool activo = m["Active"] != null && (bool)m["Active"];

            string manufacturer = DecodeWmiString((ushort[])m["ManufacturerName"]);
            string product = DecodeWmiString((ushort[])m["UserFriendlyName"]);
            string serial = DecodeWmiString((ushort[])m["SerialNumberID"]);

            Console.WriteLine("\n===============");
            Console.WriteLine($"Activo: {activo}");
            Console.WriteLine($"Fabricante: {manufacturer}");
            Console.WriteLine($"Modelo: {product}");
            Console.WriteLine($"Serial: {serial}");
        }

        static string DecodeWmiString(ushort[] data)
        {
            if (data == null) return "";

            return new string(data
                .Where(c => c != 0)
                .Select(c => (char)c)
                .ToArray());
        }

        // Impresoras
        Console.WriteLine("\n=== Impresoras ===");
        var printerSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Printer");
        foreach (ManagementObject printer in printerSearcher.Get())
        {
            string nombre = printer["Name"]?.ToString().ToLower() ?? "";

            if (nombre.Contains("7"))
                tiene7 = true;

            if (nombre.Contains("6"))
                tiene6 = true;

            if (nombre.Contains("konica"))
                tieneKonica = true;
        }

        // Resultados
        Console.WriteLine($"Tiene Impresora Bodega 7: {(tiene7 ? "SI" : "NO")}");
        Console.WriteLine($"Tiene Impresora Bodega 6: {(tiene6 ? "SI" : "NO")}");
        Console.WriteLine($"Tiene Impresora Konica: {(tieneKonica ? "SI" : "NO")}");

        Console.WriteLine("\n===== FIN =====");

        marca = marca;
        modelo = modelo;
        serialPC = serialPC;

        modeloProcesador = modeloProcesador;
        mhz = mhz;

        so = so;
        buildNumber = buildNumber;

        tiene7 = tiene7;
        tiene6 = tiene6;    
        tieneKonica = tieneKonica;
        Console.ReadLine();
    }
}