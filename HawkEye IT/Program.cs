using HawkEye_IT.Entidad;
using System.Text;

class Program
{
    static async Task Main()
    {
        // Inicio de la Recolección de Información del Equipo
        var equipo = new InformacionEquipo();
        equipo.Recolectar();

        Console.WriteLine($"{equipo.nombreUsuario} \n" +
                          $"{equipo.nombreEquipo} \n" +
                          $"{equipo.marca} \n" +
                          $"{equipo.modelo} \n" +
                          $"{equipo.serialPC} \n" +
                          $"{equipo.modeloProcesador} \n" +
                          $"{equipo.mhz} \n" +
                          $"{equipo.so} \n" +
                          $"{equipo.buildNumber} \n" +
                          $"{equipo.totalRam:F2} \n" +
                          $"{equipo.slotDetalle} \n" +
                          $"{equipo.modelAlmacenamiento} \n" +
                          $"{equipo.almacenamientoDetalle} \n" +
                          $"{equipo.redes} \n" +
                          $"{equipo.cantMonitores} \n" +
                          $"{equipo.monitores} \n" +
                          $"{equipo.tiene7}, {equipo.tiene6}, {equipo.tieneKonica}");

        // Envio de la Información del Equipo al Servidor
        string json = System.Text.Json.JsonSerializer.Serialize(equipo);
        string firma = Firma.FirmarPayload(json);

        using var http = new HttpClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.Add("X-Firma", firma);

        try
        {
            var response = await http.PostAsync("https://tuservidor.com/api/equipos/registrar", content);
            Console.WriteLine(response.IsSuccessStatusCode
                ? "\nInformación Recolectada y Almacenada Correctamente."
                : $"\nError al enviar: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError de conexión: {ex.Message}");
        }

        Console.ReadLine();
    }
}