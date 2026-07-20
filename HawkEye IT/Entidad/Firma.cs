using System.Security.Cryptography;
using System.Text;

namespace HawkEye_IT.Entidad
{
    internal class Firma
    {
        private const string ClaveSecreta = "FLDSMDFRfldsmdfr";

        public static string FirmarPayload(string jsonPayload)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(ClaveSecreta));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(jsonPayload));
            return Convert.ToHexString(hash); // ej: "A1B2C3D4..."
        }
    }
}
