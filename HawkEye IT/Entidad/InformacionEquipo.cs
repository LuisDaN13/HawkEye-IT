using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HawkEye_IT.Entidad
{
    internal class InformacionEquipo
    {
        public string Marca { get; set; }
        public string Modelo { get; set; }
        public string NumeroSerie { get; set; }
        public string Procesador { get; set; }
        public string SistemaOperativo { get; set; }
        public double TotalRam { get; set; }
        public int SlotsRam { get; set; }
        public string DiscoModelo { get; set; }
        public string DiskoBus { get; set; }
        public string DiscoTipo { get; set; }
        public long DiscoTamanoGB { get; set; }
        public double DiscoDisponibleGB { get; set; }
        public string MonitorFabricante { get; set; }
        public string MonitorModelo { get; set; }
        public string IP { get; set; }
        public string MAC { get; set; }
        public bool TieneImpresoraBodega7 { get; set; }
        public bool TieneImpresoraBodega6 { get; set; }
        public bool TieneImpresoraKonica { get; set; }
    }
}
