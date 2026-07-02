using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace HawkEye_IT.Entidad
{
    internal class InformacionEquipo
    {
        // Variables para almacenar la información del equipo
        // Usuario y Nombre del Equipo
        public string nombreUsuario = "";
        public string nombreEquipo = "";

        // Marca, Modelo y Número de Serie
        public string marca = "";
        public string modelo = "";
        public string serialPC = "";

        // Procesador y Sistema Operativo
        public string modeloProcesador = "";
        public string mhz = "";
        public string so = "";
        public string buildNumber = "";

        // RAM
        public double totalRam = 0;
        public int slotsRam = 0;
        public string slotDetalle = "";
        public StringBuilder sb = new StringBuilder();

        // Almacenamiento
        public string modelAlmacenamiento = "";
        public string almacenamientoDetalle = "";
        public StringBuilder asb = new StringBuilder();

        // Red
        public string redes = "";
        public StringBuilder redDetalle = new StringBuilder();

        // Monitor
        public int cantMonitores = 0;
        public string monitores = "";
        public StringBuilder monitorDetalle = new StringBuilder();

        // Impresoras
        public bool tiene7 = false;
        public bool tiene6 = false;
        public bool tieneKonica = false;


        // Función para recolectar la información del equipo
        public void Recolectar()
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Usuario y Nombre del Equipo
            nombreUsuario = Environment.UserName;
            nombreEquipo = Environment.MachineName;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Marca y Modelo
            var cs = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (var obj in cs.Get())
            {
                marca = obj["Manufacturer"].ToString();
                modelo = obj["Model"].ToString();
            }

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Número de Serie
            var bios = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            foreach (var obj in bios.Get())
            {
                serialPC = obj["SerialNumber"].ToString();
            }

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Procesador
            var cpu = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (var obj in cpu.Get())
            {
                modeloProcesador = obj["Name"].ToString();
                mhz = obj["MaxClockSpeed"].ToString();
            }

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Sistema Operativo
            var os = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (var obj in os.Get())
            {
                so = obj["Caption"].ToString() + " " + obj["OSArchitecture"].ToString();
                buildNumber = obj["BuildNumber"].ToString();
            }

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // RAM
            var ram = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            int slots = 0;

            foreach (var obj in ram.Get())
            {
                double capacity = Convert.ToDouble(obj["Capacity"]) / (1024 * 1024 * 1024);
                totalRam += capacity;
                slots++;

                // Hacemos la linea para caer "\n" y Agregamos el detalle al StringBuilder
                string linea = $"Slot {slots}: {capacity:F2} GB - {obj["Speed"]} MHz";
                sb.AppendLine(linea);
            }

            // Convertimos el StringBuilder a string
            slotDetalle = sb.ToString();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Almacenamiento
            var scope = new ManagementScope(@"\\.\ROOT\Microsoft\Windows\Storage");
            var query = new ObjectQuery("SELECT * FROM MSFT_PhysicalDisk");
            var searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject disk in searcher.Get())
            {
                string model = disk["Model"]?.ToString() ?? "Desconocido";
                modelAlmacenamiento = model;

                int busType = disk["BusType"] != null ? Convert.ToInt32(disk["BusType"]) : -1;
                string bus = busType switch
                {
                    17 => "NVMe",
                    11 => "SATA",
                    7 => "USB",
                    _ => "Otro"
                };

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

                if (disk["Size"] != null)
                {
                    ulong size = (ulong)disk["Size"];

                    double totalFreeGB = DriveInfo.GetDrives()
                        .Where(d => d.IsReady)
                        .Sum(d => d.TotalFreeSpace / (1024.0 * 1024 * 1024));

                    double totalLogicalGB = DriveInfo.GetDrives()
                        .Where(d => d.IsReady)
                        .Sum(d => d.TotalSize / (1024.0 * 1024 * 1024));

                    // Hacemos la linea para caer "\n" y Agregamos el detalle al StringBuilder
                    string linea = $"{size / (1024 * 1024 * 1024)} GB {tipo} {bus}";
                    asb.AppendLine(linea);
                }
                // Convertimos el StringBuilder a string
                almacenamientoDetalle = asb.ToString();
            }

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Red
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i =>
                    i.OperationalStatus == OperationalStatus.Up &&
                    i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    i.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    i.GetPhysicalAddress().GetAddressBytes().Length > 0);

            foreach (var ni in interfaces)
            {
                // MAC
                string mac = string.Join(":", ni.GetPhysicalAddress()
                    .GetAddressBytes()
                    .Select(b => b.ToString("X2")));

                // Hacemos la linea para caer "\n" y Agregamos el detalle al StringBuilder
                string linea = $"Interfaz: {ni.Name} - MAC: {mac}";
                redDetalle.AppendLine(linea);
            }

            // Convertimos el StringBuilder a string
            redes = redDetalle.ToString();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Monitor
            var scopeMonitor = new ManagementScope(@"\\.\root\wmi");
            var queryMonitor = new ObjectQuery("SELECT * FROM WmiMonitorID");
            var searcherMonitor = new ManagementObjectSearcher(scopeMonitor, queryMonitor);

            foreach (ManagementObject m in searcherMonitor.Get())
            {
                cantMonitores += 1;
                string manufacturer = DecodeWmiString((ushort[])m["ManufacturerName"]);
                string product = DecodeWmiString((ushort[])m["UserFriendlyName"]);
                if (product == "") product = "Integrado";
                string serial = DecodeWmiString((ushort[])m["SerialNumberID"]);

                // Hacemos la linea para caer "\n" y Agregamos el detalle al StringBuilder
                string linea = $"Marca: {manufacturer} - Modelo: {product} - Serial: {serial}";
                monitorDetalle.AppendLine(linea);
            }

            static string DecodeWmiString(ushort[] data)
            {
                if (data == null) return "";

                return new string(data
                    .Where(c => c != 0)
                    .Select(c => (char)c)
                    .ToArray());
            }

            // Convertimos el StringBuilder a string
            monitores = monitorDetalle.ToString();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Impresoras
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
        }
    }
}
