using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RJP.Signer.Bridge
{
    internal static class Program
    {
        private const int Port = 17341;
        private const int MaxBody = 250 * 1024 * 1024;
        private const string Version = "1.1.0";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private static readonly HashSet<string> AllowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://ruijpedro.github.io",
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost",
            "capacitor://localhost"
        };

        private static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "RJP Signer Bridge";
            Console.WriteLine("========================================");
            Console.WriteLine(" RJP SIGNER BRIDGE v" + Version);
            Console.WriteLine(" Cartão de Cidadão / Certificados Windows");
            Console.WriteLine("========================================");
            Console.WriteLine("Ligação local: http://127.0.0.1:" + Port);
            Console.WriteLine("Não feches esta janela enquanto estiveres a assinar.");
            Console.WriteLine();

            var listener = new TcpListener(IPAddress.Loopback, Port);
            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("ERRO: não foi possível abrir a porta " + Port + ".");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Verifica se já tens outro RJP Signer Bridge aberto.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Bridge pronto. Abre o RJP Signer no Chrome/Edge.");
            while (true)
            {
                var client = listener.AcceptTcpClient();
                Task.Run(() => HandleClient(client));
            }
        }

        private static void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    var req = HttpRequest.Read(stream, MaxBody);
                    if (req == null) return;

                    var origin = req.Header("Origin");
                    if (!string.IsNullOrWhiteSpace(origin) && !AllowedOrigins.Contains(origin))
                    {
                        WriteJson(stream, 403, new { ok = false, error = "Origem não autorizada: " + origin }, origin: null);
                        return;
                    }

                    if (req.Method == "OPTIONS")
                    {
                        WriteResponse(stream, 204, "text/plain", new byte[0], origin);
                        return;
                    }

                    if (req.Method == "GET" && req.Path == "/health")
                    {
                        WriteJson(stream, 200, new
                        {
                            ok = true,
                            name = "RJP Signer Bridge",
                            version = Version,
                            platform = Environment.OSVersion.VersionString,
                            capabilities = new[] { "dwfx-sign", "certificate-list", "opc-verify" },
                            compatibility = "Autodesk/OPC RSA-SHA1"
                        }, origin);
                        return;
                    }

                    if (req.Method == "GET" && req.Path == "/certificates")
                    {
                        WriteJson(stream, 200, new { ok = true, certificates = GetCertificates() }, origin);
                        return;
                    }

                    if (req.Method == "POST" && req.Path == "/sign/dwfx")
                    {
                        SignDwfx(req, stream, origin);
                        return;
                    }

                    WriteJson(stream, 404, new { ok = false, error = "Endpoint não encontrado." }, origin);
                }
                catch (Exception ex)
                {
                    try { WriteJson(stream, 500, new { ok = false, error = CleanError(ex) }, null); } catch { }
                    Console.WriteLine("ERRO: " + ex);
                }
            }
        }

        private static object[] GetCertificates()
        {
            var list = new List<object>();
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var cert in store.Certificates.Cast<X509Certificate2>())
                {
                    if (!cert.HasPrivateKey) continue;
                    var now = DateTime.Now;
                    var valid = cert.NotBefore <= now && cert.NotAfter >= now;
                    var keyUsage = GetKeyUsage(cert);
                    var recommended = valid && keyUsage.HasFlag(X509KeyUsageFlags.NonRepudiation);
                    list.Add(new
                    {
                        thumbprint = NormalizeThumbprint(cert.Thumbprint),
                        subject = FriendlySubject(cert),
                        issuer = cert.Issuer,
                        notBefore = cert.NotBefore.ToString("o"),
                        notAfter = cert.NotAfter.ToString("o"),
                        valid,
                        keyUsage = keyUsage.ToString(),
                        recommended,
                        citizenCard = LooksLikeCitizenCard(cert)
                    });
                }
            }

            return list.OrderByDescending(x => GetBool(x, "recommended"))
                       .ThenByDescending(x => GetBool(x, "citizenCard"))
                       .ThenBy(x => GetString(x, "subject"))
                       .ToArray();
        }

        private static void SignDwfx(HttpRequest req, NetworkStream stream, string origin)
        {
            var thumbprint = NormalizeThumbprint(req.Header("X-RJP-Certificate"));
            var filename = SafeFileName(req.Header("X-RJP-Filename"));
            if (string.IsNullOrWhiteSpace(thumbprint)) throw new InvalidOperationException("Seleciona um certificado de assinatura.");
            if (req.Body == null || req.Body.Length < 4) throw new InvalidDataException("DWFx vazio ou inválido.");
            if (req.Body[0] != 0x50 || req.Body[1] != 0x4B) throw new InvalidDataException("O ficheiro não parece ser um DWFx/OPC válido.");

            var cert = FindCertificate(thumbprint);
            if (cert == null) throw new InvalidOperationException("O certificado escolhido já não está disponível no Windows.");
            if (!cert.HasPrivateKey) throw new InvalidOperationException("O certificado não tem uma chave privada acessível.");
            if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter) throw new InvalidOperationException("O certificado está fora do período de validade.");

            var temp = Path.Combine(Path.GetTempPath(), "RJP_Signer_" + Guid.NewGuid().ToString("N") + ".dwfx");
            File.WriteAllBytes(temp, req.Body);
            try
            {
                Console.WriteLine("A assinar: " + filename);
                Console.WriteLine("Certificado: " + FriendlySubject(cert));
                Console.WriteLine("Se o Cartão de Cidadão pedir o PIN, introduz apenas na janela oficial do middleware.");

                using (var package = Package.Open(temp, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var manager = new PackageDigitalSignatureManager(package);
                    if (manager.IsSigned) throw new InvalidOperationException("Este DWFx já contém uma assinatura. A V1.1 evita alterar ficheiros já assinados.");

                    manager.CertificateOption = CertificateEmbeddingOption.InCertificatePart;
                    manager.HashAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1";
                    manager.TimeFormat = "YYYY-MM-DDThh:mm:ss.sTZD";

                    // O Design Review de referência assina todos os PackagePart, exceto os raster overlays TIFF.
                    // [Content_Types].xml não é um PackagePart e é automaticamente excluído pelo OPC.
                    var toSign = package.GetParts()
                        .Where(p => !IsSignatureInfrastructure(p.Uri))
                        .Where(p => !IsRasterOverlayTiff(p.Uri))
                        .Select(p => p.Uri)
                        .ToList();

                    if (toSign.Count == 0) throw new InvalidDataException("O DWFx não contém partes assináveis.");

                    manager.Sign(toSign, cert, new List<PackageRelationshipSelector>(), "SignatureIdValue");
                    package.Flush();

                    if (!manager.IsSigned) throw new CryptographicException("A assinatura não foi criada.");
                    var result = manager.VerifySignatures(false);
                    if (result != VerifyResult.Success) throw new CryptographicException("A assinatura foi criada mas a verificação OPC falhou: " + result);
                }

                var signedBytes = File.ReadAllBytes(temp);
                var outputName = BuildOutputName(filename);
                var headers = new Dictionary<string, string>
                {
                    ["Content-Disposition"] = "attachment; filename=\"" + outputName.Replace("\"", "") + "\"",
                    ["X-RJP-Output-Name"] = Uri.EscapeDataString(outputName),
                    ["X-RJP-Signer"] = Uri.EscapeDataString(FriendlySubject(cert)),
                    ["X-RJP-Bridge-Version"] = Version
                };
                WriteResponse(stream, 200, "application/octet-stream", signedBytes, origin, headers);
                Console.WriteLine("OK: assinatura criada e verificada -> " + outputName);
                Console.WriteLine();
            }
            finally
            {
                try { File.Delete(temp); } catch { }
                cert.Dispose();
            }
        }

        private static X509Certificate2 FindCertificate(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var cert in store.Certificates.Cast<X509Certificate2>())
                {
                    if (NormalizeThumbprint(cert.Thumbprint) == thumbprint)
                        return new X509Certificate2(cert);
                }
            }
            return null;
        }

        private static bool IsSignatureInfrastructure(Uri uri)
        {
            return uri.OriginalString.StartsWith("/package/services/digital-signature/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRasterOverlayTiff(Uri uri)
        {
            var p = uri.OriginalString;
            return p.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase);
        }

        private static X509KeyUsageFlags GetKeyUsage(X509Certificate2 cert)
        {
            foreach (var ext in cert.Extensions)
                if (ext is X509KeyUsageExtension ku) return ku.KeyUsages;
            return X509KeyUsageFlags.None;
        }

        private static bool LooksLikeCitizenCard(X509Certificate2 cert)
        {
            var s = (cert.Subject + " " + cert.Issuer + " " + cert.FriendlyName).ToUpperInvariant();
            return s.Contains("CARTAO") || s.Contains("CARTÃO") || s.Contains("CITIZEN") || s.Contains("EC DE ASSINATURA DIGITAL QUALIFICADA") || s.Contains("AUTENTICACAO.GOV");
        }

        private static string FriendlySubject(X509Certificate2 cert)
        {
            var name = cert.GetNameInfo(X509NameType.SimpleName, false);
            return string.IsNullOrWhiteSpace(name) ? cert.Subject : name;
        }

        private static string NormalizeThumbprint(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return new string(s.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }

        private static string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "documento.dwfx";
            name = Uri.UnescapeDataString(name);
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name;
        }

        private static string BuildOutputName(string name)
        {
            var ext = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".dwfx";
            return stem + "_ASSINADO" + ext;
        }

        private static string CleanError(Exception ex)
        {
            if (ex is AggregateException a && a.InnerException != null) ex = a.InnerException;
            if (ex.InnerException != null && ex.Message.Contains("invocation")) ex = ex.InnerException;
            return ex.Message;
        }

        private static void WriteJson(NetworkStream stream, int status, object value, string origin)
        {
            var bytes = Encoding.UTF8.GetBytes(Json.Serialize(value));
            WriteResponse(stream, status, "application/json; charset=utf-8", bytes, origin);
        }

        private static void WriteResponse(NetworkStream stream, int status, string contentType, byte[] body, string origin, Dictionary<string, string> extra = null)
        {
            var reason = status == 200 ? "OK" : status == 204 ? "No Content" : status == 400 ? "Bad Request" : status == 403 ? "Forbidden" : status == 404 ? "Not Found" : "Internal Server Error";
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
            sb.Append("Content-Type: ").Append(contentType).Append("\r\n");
            sb.Append("Content-Length: ").Append(body?.Length ?? 0).Append("\r\n");
            sb.Append("Cache-Control: no-store\r\n");
            sb.Append("Connection: close\r\n");
            if (!string.IsNullOrWhiteSpace(origin) && AllowedOrigins.Contains(origin))
                sb.Append("Access-Control-Allow-Origin: ").Append(origin).Append("\r\n");
            sb.Append("Vary: Origin\r\n");
            sb.Append("Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n");
            sb.Append("Access-Control-Allow-Headers: Content-Type, X-RJP-Certificate, X-RJP-Filename\r\n");
            sb.Append("Access-Control-Expose-Headers: X-RJP-Output-Name, X-RJP-Signer, X-RJP-Bridge-Version\r\n");
            sb.Append("Access-Control-Allow-Private-Network: true\r\n");
            if (extra != null) foreach (var kv in extra) sb.Append(kv.Key).Append(": ").Append(kv.Value).Append("\r\n");
            sb.Append("\r\n");
            var header = Encoding.ASCII.GetBytes(sb.ToString());
            stream.Write(header, 0, header.Length);
            if (body != null && body.Length > 0) stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private static bool GetBool(object o, string prop)
        {
            var p = o.GetType().GetProperty(prop);
            return p != null && (bool)p.GetValue(o, null);
        }
        private static string GetString(object o, string prop)
        {
            var p = o.GetType().GetProperty(prop);
            return p == null ? "" : (string)p.GetValue(o, null);
        }
    }

    internal sealed class HttpRequest
    {
        public string Method { get; private set; }
        public string Path { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public byte[] Body { get; private set; }

        public string Header(string name) => Headers.TryGetValue(name, out var v) ? v : null;

        public static HttpRequest Read(NetworkStream stream, int maxBody)
        {
            var headerBytes = ReadUntilHeaderEnd(stream, 64 * 1024);
            if (headerBytes == null || headerBytes.Length == 0) return null;
            var text = Encoding.ASCII.GetString(headerBytes);
            var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var first = lines[0].Split(' ');
            if (first.Length < 2) throw new InvalidDataException("Pedido HTTP inválido.");
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < lines.Length; i++)
            {
                var idx = lines[i].IndexOf(':');
                if (idx <= 0) continue;
                headers[lines[i].Substring(0, idx).Trim()] = lines[i].Substring(idx + 1).Trim();
            }
            var len = 0;
            if (headers.TryGetValue("Content-Length", out var cl) && !int.TryParse(cl, out len)) throw new InvalidDataException("Content-Length inválido.");
            if (len < 0 || len > maxBody) throw new InvalidDataException("Ficheiro demasiado grande para o Bridge.");
            var body = new byte[len];
            var off = 0;
            while (off < len)
            {
                var n = stream.Read(body, off, len - off);
                if (n <= 0) throw new EndOfStreamException("Ligação interrompida durante o envio do ficheiro.");
                off += n;
            }
            var rawPath = first[1];
            var q = rawPath.IndexOf('?');
            if (q >= 0) rawPath = rawPath.Substring(0, q);
            return new HttpRequest { Method = first[0].ToUpperInvariant(), Path = rawPath, Headers = headers, Body = body };
        }

        private static byte[] ReadUntilHeaderEnd(NetworkStream stream, int max)
        {
            using (var ms = new MemoryStream())
            {
                int state = 0;
                while (ms.Length < max)
                {
                    var b = stream.ReadByte();
                    if (b < 0) return ms.ToArray();
                    ms.WriteByte((byte)b);
                    state = state == 0 && b == '\r' ? 1 :
                            state == 1 && b == '\n' ? 2 :
                            state == 2 && b == '\r' ? 3 :
                            state == 3 && b == '\n' ? 4 :
                            b == '\r' ? 1 : 0;
                    if (state == 4)
                    {
                        var a = ms.ToArray();
                        Array.Resize(ref a, a.Length - 4);
                        return a;
                    }
                }
                throw new InvalidDataException("Cabeçalhos HTTP demasiado grandes.");
            }
        }
    }
}
