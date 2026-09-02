using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using System.Windows.Forms;

namespace RJP.Signer.Bridge
{
    internal static class Program
    {
        private const int Port = 17341;
        private const int MaxBody = 250 * 1024 * 1024;
        private const string Version = "1.3.0";
        private const string DefaultWebAppUrl = "https://ruijpedro.github.io/RJP_Signer/";
        private const string LegacyRsaSha1SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private static readonly HashSet<string> AllowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://ruijpedro.github.io",
            "http://localhost:5173",
            "http://127.0.0.1:5173",
            "http://localhost",
            "https://localhost",
            "capacitor://localhost"
        };

        private static TcpListener Listener;
        private static NotifyIcon Tray;
        private static Mutex SingleInstance;
        private static volatile bool Stopping;
        private static string PairCode;
        private static string PairToken;
        private static string DataDir;
        private static string TokenPath;
        private static string LogPath;

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool created;
            SingleInstance = new Mutex(true, "Local\\RJP_Signer_Bridge_V1", out created);
            if (!created)
            {
                MessageBox.Show("O RJP Signer Bridge já está em execução junto ao relógio do Windows.", "RJP Signer Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PrepareState();
            if (!StartServer()) return;
            BuildTray();
            Log("Bridge iniciado. Porta " + Port + ".");
            Tray.ShowBalloonTip(3500, "RJP Signer Bridge", "Bridge pronto. Código de emparelhamento disponível no ícone junto ao relógio.", ToolTipIcon.Info);
            Application.Run();
            StopServer();
            if (Tray != null) { Tray.Visible = false; Tray.Dispose(); }
            SingleInstance.ReleaseMutex();
            SingleInstance.Dispose();
        }

        private static void PrepareState()
        {
            DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RJP Signer Bridge");
            Directory.CreateDirectory(DataDir);
            TokenPath = Path.Combine(DataDir, "pair.token");
            LogPath = Path.Combine(DataDir, "bridge.log");
            PairToken = LoadOrCreateToken();
            RotatePairCode();
        }

        private static void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Abrir RJP Signer", null, (s, e) => OpenWebApp());
            menu.Items.Add("Mostrar código de emparelhamento", null, (s, e) => ShowPairCode());
            menu.Items.Add("Copiar código", null, (s, e) => { Clipboard.SetText(PairCode); Tray.ShowBalloonTip(1800, "RJP Signer Bridge", "Código copiado.", ToolTipIcon.Info); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Atualizar código", null, (s, e) => { RotatePairCode(); ShowPairCode(); });
            menu.Items.Add("Revogar browsers emparelhados", null, (s, e) => RevokePairings());
            menu.Items.Add("Abrir pasta de dados", null, (s, e) => Process.Start(new ProcessStartInfo(DataDir) { UseShellExecute = true }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Sair", null, (s, e) => Application.Exit());

            Tray = new NotifyIcon
            {
                Icon = SystemIcons.Shield,
                Text = "RJP Signer Bridge",
                Visible = true,
                ContextMenuStrip = menu
            };
            Tray.DoubleClick += (s, e) => OpenWebApp();
        }

        private static void OpenWebApp()
        {
            try { Process.Start(new ProcessStartInfo(GetWebAppUrl()) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "RJP Signer Bridge", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private static string GetWebAppUrl()
        {
            try
            {
                var configured = ConfigurationManager.AppSettings["WebAppUrl"];
                return string.IsNullOrWhiteSpace(configured) ? DefaultWebAppUrl : configured.Trim();
            }
            catch { return DefaultWebAppUrl; }
        }

        private static void ShowPairCode()
        {
            MessageBox.Show(
                "Código de emparelhamento:\n\n" + PairCode + "\n\nIntroduz este código apenas na WebApp RJP Signer aberta por ti. O código muda quando reinicias/atualizas o Bridge.",
                "RJP Signer Bridge", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void RevokePairings()
        {
            var confirm = MessageBox.Show("Revogar todos os browsers emparelhados? Será necessário introduzir um novo código na WebApp.", "RJP Signer Bridge", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            PairToken = NewToken(32);
            File.WriteAllText(TokenPath, PairToken, Encoding.ASCII);
            RotatePairCode();
            Log("Emparelhamentos revogados.");
            ShowPairCode();
        }

        private static string LoadOrCreateToken()
        {
            try
            {
                if (File.Exists(TokenPath))
                {
                    var current = File.ReadAllText(TokenPath).Trim();
                    if (current.Length >= 32) return current;
                }
            }
            catch { }
            var token = NewToken(32);
            File.WriteAllText(TokenPath, token, Encoding.ASCII);
            return token;
        }

        private static void RotatePairCode()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var b = new byte[4]; rng.GetBytes(b);
                var value = BitConverter.ToUInt32(b, 0) % 1000000;
                PairCode = value.ToString("D6");
            }
        }

        private static string NewToken(int bytes)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var b = new byte[bytes]; rng.GetBytes(b);
                return BitConverter.ToString(b).Replace("-", "");
            }
        }

        private static bool StartServer()
        {
            Listener = new TcpListener(IPAddress.Loopback, Port);
            try { Listener.Start(); }
            catch (SocketException ex)
            {
                MessageBox.Show("Não foi possível abrir a porta local " + Port + ".\n\n" + ex.Message + "\n\nConfirma que não existe outro Bridge aberto.", "RJP Signer Bridge", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            Task.Run(() => AcceptLoop());
            return true;
        }

        private static void StopServer()
        {
            Stopping = true;
            try { if (Listener != null) Listener.Stop(); } catch { }
        }

        private static void AcceptLoop()
        {
            while (!Stopping)
            {
                try
                {
                    var client = Listener.AcceptTcpClient();
                    Task.Run(() => HandleClient(client));
                }
                catch (SocketException) { if (!Stopping) Thread.Sleep(100); }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { Log("AcceptLoop: " + ex); Thread.Sleep(200); }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                string origin = null;
                try
                {
                    client.ReceiveTimeout = 120000;
                    client.SendTimeout = 120000;
                    var req = HttpRequest.Read(stream, MaxBody);
                    if (req == null) return;
                    origin = req.Header("Origin");
                    if (!string.IsNullOrWhiteSpace(origin) && !AllowedOrigins.Contains(origin))
                    {
                        WriteJson(stream, 403, new { ok = false, error = "Origem não autorizada." }, null);
                        return;
                    }

                    if (req.Method == "OPTIONS") { WriteResponse(stream, 204, "text/plain", new byte[0], origin); return; }
                    if (req.Method == "GET" && req.Path == "/health")
                    {
                        WriteJson(stream, 200, new
                        {
                            ok = true, name = "RJP Signer Bridge", version = Version,
                            capabilities = new[] { "dwfx-sign", "dwfx-verify", "certificate-list", "pairing", "save-as-dialog" },
                            compatibility = "Autodesk/OPC RSA-SHA1 via PKCS#11 Autenticação.gov", pairingRequired = true
                        }, origin); return;
                    }
                    if (req.Method == "POST" && req.Path == "/pair") { HandlePair(req, stream, origin); return; }
                    if (!IsAuthorized(req)) { WriteJson(stream, 401, new { ok = false, error = "Bridge não emparelhado ou token expirado." }, origin); return; }
                    if (req.Method == "GET" && req.Path == "/certificates") { WriteJson(stream, 200, new { ok = true, certificates = GetCertificates() }, origin); return; }
                    if (req.Method == "POST" && req.Path == "/verify/dwfx") { VerifyDwfx(req, stream, origin); return; }
                    if (req.Method == "POST" && req.Path == "/sign/dwfx") { SignDwfx(req, stream, origin); return; }
                    WriteJson(stream, 404, new { ok = false, error = "Endpoint não encontrado." }, origin);
                }
                catch (Exception ex)
                {
                    Log("Pedido: " + ex);
                    try { WriteJson(stream, 500, new { ok = false, error = CleanError(ex) }, origin); } catch { }
                }
            }
        }

        private static void HandlePair(HttpRequest req, NetworkStream stream, string origin)
        {
            var code = (req.Header("X-RJP-Pair-Code") ?? "").Trim();
            if (!FixedEquals(code, PairCode))
            {
                Thread.Sleep(500);
                WriteJson(stream, 400, new { ok = false, error = "Código de emparelhamento incorreto." }, origin);
                return;
            }
            Log("Novo browser emparelhado para origem " + (origin ?? "sem Origin") + ".");
            WriteJson(stream, 200, new { ok = true, token = PairToken, version = Version }, origin);
            RotatePairCode();
        }

        private static bool IsAuthorized(HttpRequest req)
        {
            var supplied = (req.Header("X-RJP-Token") ?? "").Trim();
            return supplied.Length > 0 && FixedEquals(supplied, PairToken);
        }

        private static bool FixedEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            var aa = Encoding.UTF8.GetBytes(a); var bb = Encoding.UTF8.GetBytes(b);
            var diff = aa.Length ^ bb.Length;
            var n = Math.Max(aa.Length, bb.Length);
            for (var i = 0; i < n; i++)
            {
                var av = i < aa.Length ? aa[i] : (byte)0;
                var bv = i < bb.Length ? bb[i] : (byte)0;
                diff |= av ^ bv;
            }
            return diff == 0;
        }

        private static CertificateInfo[] GetCertificates()
        {
            var list = new List<CertificateInfo>();
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var cert in store.Certificates.Cast<X509Certificate2>())
                {
                    if (!cert.HasPrivateKey) continue;
                    var now = DateTime.Now;
                    var valid = cert.NotBefore <= now && cert.NotAfter >= now;
                    var keyUsage = GetKeyUsage(cert);
                    var recommended = valid && (keyUsage.HasFlag(X509KeyUsageFlags.NonRepudiation) || keyUsage.HasFlag(X509KeyUsageFlags.DigitalSignature));
                    list.Add(new CertificateInfo
                    {
                        thumbprint = NormalizeThumbprint(cert.Thumbprint), subject = FriendlySubject(cert), issuer = cert.Issuer,
                        notBefore = cert.NotBefore.ToString("o"), notAfter = cert.NotAfter.ToString("o"), valid = valid,
                        keyUsage = keyUsage.ToString(), recommended = recommended, citizenCard = LooksLikeCitizenCard(cert)
                    });
                }
            }
            return list.OrderByDescending(x => x.valid && x.citizenCard && x.recommended)
                       .ThenByDescending(x => x.valid && x.recommended)
                       .ThenByDescending(x => x.citizenCard)
                       .ThenBy(x => x.subject).ToArray();
        }

        private static void SignDwfx(HttpRequest req, NetworkStream stream, string origin)
        {
            var thumbprint = NormalizeThumbprint(req.Header("X-RJP-Certificate"));
            var filename = SafeFileName(req.Header("X-RJP-Filename"));
            var mode = (req.Header("X-RJP-Sign-Mode") ?? "autodesk-compat").Trim();
            if (!string.Equals(mode, "autodesk-compat", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Modo de assinatura não suportado nesta versão.");
            ValidateDwfxRequest(req);
            if (string.IsNullOrWhiteSpace(thumbprint)) throw new InvalidOperationException("Seleciona um certificado de assinatura.");

            var cert = FindCertificate(thumbprint);
            if (cert == null) throw new InvalidOperationException("O certificado escolhido já não está disponível no Windows.");
            try
            {
                if (!cert.HasPrivateKey) throw new InvalidOperationException("O certificado não tem uma chave privada acessível.");
                if (DateTime.Now < cert.NotBefore || DateTime.Now > cert.NotAfter) throw new InvalidOperationException("O certificado está fora do período de validade.");
                using (var rsaPublic = cert.GetRSAPublicKey())
                {
                    if (rsaPublic == null)
                        throw new NotSupportedException("Este certificado não é RSA. O modo Autodesk/Design Review legado exige RSA-SHA1. PDF/PAdES moderno não terá esta limitação.");
                }

                var answer = MessageBox.Show(
                    "Confirmas a assinatura digital deste ficheiro?\n\n" + filename + "\n\nCertificado:\n" + FriendlySubject(cert) +
                    "\n\nModo: Compatibilidade Autodesk/Design Review\nMotor: PKCS#11 Autenticação.gov\n\nO PIN deve ser introduzido apenas na janela protegida do Cartão de Cidadão/token.",
                    "RJP Signer — Confirmar assinatura", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes) throw new OperationCanceledException("Assinatura cancelada pelo utilizador no Windows.");

                var outputName = BuildOutputName(filename);
                var savePath = ChooseSavePath(outputName);
                if (string.IsNullOrWhiteSpace(savePath)) throw new OperationCanceledException("Guardar como cancelado. Nenhuma cópia assinada foi gravada.");

                var temp = TempFile(".dwfx");
                File.WriteAllBytes(temp, req.Body);
                try
                {
                    VerifyResult verifyResult;
                    X509ChainStatusFlags certStatus;
                    int signatureCount;
                    int signedParts;
                    DateTime signedAt;
                    string signer;
                    string pkcs11Module = ResolvePkcs11Module();

                    // Fase 1 — construir a infraestrutura OPC com um certificado temporário de software.
                    // Isto evita pedir ao Cartão de Cidadão uma assinatura RSA-SHA256 que depois seria descartada.
                    // Em seguida, o SignedInfo é mudado para rsa-sha1 e assinado diretamente via PKCS#11 oficial.
                    using (var package = Package.Open(temp, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    using (var placeholderRsa = CreatePlaceholderRsa())
                    {
                        var request = new CertificateRequest(
                            "CN=RJP Signer OPC Placeholder",
                            placeholderRsa,
                            HashAlgorithmName.SHA256,
                            RSASignaturePadding.Pkcs1);
                        using (var placeholderCert = request.CreateSelfSigned(DateTimeOffset.Now.AddMinutes(-5), DateTimeOffset.Now.AddDays(1)))
                        {
                            var manager = new PackageDigitalSignatureManager(package);
                            if (manager.IsSigned) throw new InvalidOperationException("Este DWFx já contém uma assinatura. A V1.3 não altera documentos DWFx já assinados.");
                            manager.CertificateOption = CertificateEmbeddingOption.InCertificatePart;
                            manager.HashAlgorithm = "http://www.w3.org/2000/09/xmldsig#sha1";
                            manager.TimeFormat = "YYYY-MM-DDThh:mm:ss.sTZD";
                            var toSign = package.GetParts()
                                .Where(p => !IsSignatureInfrastructure(p.Uri))
                                .Where(p => !IsRasterOverlayTiff(p.Uri))
                                .Select(p => p.Uri)
                                .ToList();
                            if (toSign.Count == 0) throw new InvalidDataException("O DWFx não contém partes assináveis.");

                            var created = manager.Sign(toSign, placeholderCert, new List<PackageRelationshipSelector>(), "SignatureIdValue");
                            if (created == null || created.SignaturePart == null) throw new CryptographicException("O motor OPC não criou a infraestrutura de assinatura.");

                            // Troca o certificado temporário pelo certificado real do Cartão de Cidadão.
                            ReplaceEmbeddedCertificate(package, cert.RawData);

                            // Mantém o XML do PackageDigitalSignatureManager byte-a-byte o mais estável possível:
                            // apenas SignatureMethod e SignatureValue são alterados.
                            var signatureXml = ReadPartText(created.SignaturePart);
                            signatureXml = ReplaceSignatureMethod(signatureXml, LegacyRsaSha1SignatureMethod);
                            var canonicalSignedInfo = CanonicalizeSignedInfo(signatureXml);

                            // Assina o SignedInfo canónico com o módulo oficial Autenticação.gov.
                            var pkcs11Signature = SignRsaSha1WithCitizenCardPkcs11(cert, canonicalSignedInfo, pkcs11Module);
                            VerifyRsaSha1Locally(cert, canonicalSignedInfo, pkcs11Signature);
                            signatureXml = ReplaceSignatureValue(signatureXml, Convert.ToBase64String(pkcs11Signature));
                            WritePartText(created.SignaturePart, signatureXml);

                            var actualMethod = ReadSignatureMethod(created.SignaturePart);
                            if (!string.Equals(actualMethod, LegacyRsaSha1SignatureMethod, StringComparison.Ordinal))
                                throw new CryptographicException("O SignatureMethod final não ficou em rsa-sha1.");

                            package.Flush();
                        }
                    }

                    // Fase 2 — reabrir o DWFx e validar com o mesmo verificador OPC que já validou o ficheiro Autodesk.
                    using (var package = Package.Open(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var manager = new PackageDigitalSignatureManager(package);
                        if (!manager.IsSigned) throw new CryptographicException("O package foi fechado mas a assinatura não ficou persistida.");

                        verifyResult = manager.VerifySignatures(false);
                        var signatures = manager.Signatures.ToList();
                        var first = signatures.FirstOrDefault();
                        signatureCount = signatures.Count;
                        signedParts = signatures.Sum(sig => sig.SignedParts.Count);
                        signedAt = first == null ? DateTime.Now : first.SigningTime;
                        signer = first != null && first.Signer != null
                            ? FriendlySubject(new X509Certificate2(first.Signer))
                            : FriendlySubject(cert);
                        certStatus = first != null && first.Signer != null
                            ? PackageDigitalSignatureManager.VerifyCertificate(first.Signer)
                            : X509ChainStatusFlags.NotSignatureValid;

                        if (verifyResult != VerifyResult.Success)
                        {
                            var diagnosticPath = BuildDiagnosticSavePath(savePath);
                            File.WriteAllBytes(diagnosticPath, File.ReadAllBytes(temp));
                            var reportPath = diagnosticPath + ".txt";
                            File.WriteAllText(reportPath,
                                "RJP Signer — diagnóstico de assinatura DWFx" + Environment.NewLine +
                                "Versão Bridge: " + Version + Environment.NewLine +
                                "Motor: PKCS#11 Autenticação.gov" + Environment.NewLine +
                                "Módulo: " + pkcs11Module + Environment.NewLine +
                                "Ficheiro origem: " + filename + Environment.NewLine +
                                "Resultado OPC: " + verifyResult + Environment.NewLine +
                                "Assinaturas: " + signatureCount + Environment.NewLine +
                                "Partes protegidas: " + signedParts + Environment.NewLine +
                                "Signatário: " + signer + Environment.NewLine +
                                "Data: " + signedAt.ToString("o") + Environment.NewLine +
                                "Certificado: " + certStatus + Environment.NewLine +
                                "IMPORTANTE: este ficheiro NÃO deve ser usado como documento assinado válido." + Environment.NewLine,
                                Encoding.UTF8);
                            Log("PKCS11: verificação final falhou: " + verifyResult + " | diagnóstico=" + diagnosticPath);
                            throw new CryptographicException(
                                "A assinatura PKCS#11 foi criada mas a verificação OPC falhou: " + verifyResult +
                                ". Foi guardada uma cópia de diagnóstico em: " + diagnosticPath + ".");
                        }
                    }

                    var signedBytes = File.ReadAllBytes(temp);
                    File.WriteAllBytes(savePath, signedBytes);
                    var savedName = Path.GetFileName(savePath);
                    var headers = new Dictionary<string, string>
                    {
                        ["Content-Disposition"] = "attachment; filename=\"" + outputName.Replace("\"", "") + "\"",
                        ["X-RJP-Output-Name"] = Uri.EscapeDataString(outputName),
                        ["X-RJP-Signer"] = Uri.EscapeDataString(signer),
                        ["X-RJP-Bridge-Version"] = Version,
                        ["X-RJP-Verify-Result"] = Uri.EscapeDataString(verifyResult.ToString()),
                        ["X-RJP-Signed-Parts"] = signedParts.ToString(),
                        ["X-RJP-Signature-Count"] = signatureCount.ToString(),
                        ["X-RJP-Algorithm"] = Uri.EscapeDataString("RSA-SHA1 / SHA-1 (Autodesk compat via PKCS#11)"),
                        ["X-RJP-Signed-At"] = Uri.EscapeDataString(signedAt.ToString("o")),
                        ["X-RJP-Certificate-Status"] = Uri.EscapeDataString(certStatus.ToString()),
                        ["X-RJP-Saved"] = "1",
                        ["X-RJP-Saved-Name"] = Uri.EscapeDataString(savedName)
                    };
                    WriteResponse(stream, 200, "application/octet-stream", signedBytes, origin, headers);
                    Log("PKCS11: assinado e guardado: " + filename + " -> " + savePath + " | " + signer + " | partes=" + signedParts + " | OPC=" + verifyResult);
                }
                finally { try { File.Delete(temp); } catch { } }
            }
            finally { cert.Dispose(); }
        }

        private static RSACryptoServiceProvider CreatePlaceholderRsa()
        {
            var csp = new CspParameters(24)
            {
                KeyContainerName = "RJP_Signer_OPC_" + Guid.NewGuid().ToString("N"),
                Flags = CspProviderFlags.NoPrompt
            };
            var rsa = new RSACryptoServiceProvider(2048, csp);
            rsa.PersistKeyInCsp = false;
            return rsa;
        }

        private static string ResolvePkcs11Module()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.SystemDirectory, "pteidpkcs11.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "pteidpkcs11.dll")
            };
            foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
                if (File.Exists(candidate)) return candidate;
            throw new FileNotFoundException(
                "Não foi encontrado o módulo PKCS#11 do Autenticação.gov (pteidpkcs11.dll). Instala/atualiza a aplicação Autenticação.gov antes de assinar.",
                candidates[0]);
        }

        private static void ReplaceEmbeddedCertificate(Package package, byte[] certificateRawData)
        {
            var certParts = package.GetParts()
                .Where(p => p.Uri.OriginalString.IndexOf("/package/services/digital-signature/certificate/", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            if (certParts.Count != 1) throw new InvalidDataException("Era esperado exatamente um CertificatePart OPC; encontrados: " + certParts.Count + ".");
            using (var output = certParts[0].GetStream(FileMode.Create, FileAccess.Write))
                output.Write(certificateRawData, 0, certificateRawData.Length);
        }

        private static string ReadPartText(PackagePart part)
        {
            using (var input = part.GetStream(FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(input, Encoding.UTF8, true, 4096, false))
                return reader.ReadToEnd();
        }

        private static void WritePartText(PackagePart part, string value)
        {
            var bytes = new UTF8Encoding(true).GetBytes(value);
            using (var output = part.GetStream(FileMode.Create, FileAccess.Write))
                output.Write(bytes, 0, bytes.Length);
        }

        private static string ReplaceSignatureMethod(string xml, string algorithm)
        {
            const string marker = "<SignatureMethod Algorithm=\"";
            var start = xml.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException("SignatureMethod não encontrado no XMLDSIG.");
            start += marker.Length;
            var end = xml.IndexOf('"', start);
            if (end <= start) throw new InvalidDataException("SignatureMethod inválido no XMLDSIG.");
            return xml.Substring(0, start) + algorithm + xml.Substring(end);
        }

        private static string ReplaceSignatureValue(string xml, string base64Signature)
        {
            const string open = "<SignatureValue>";
            const string close = "</SignatureValue>";
            var start = xml.IndexOf(open, StringComparison.Ordinal);
            if (start < 0) throw new InvalidDataException("SignatureValue não encontrado no XMLDSIG.");
            start += open.Length;
            var end = xml.IndexOf(close, start, StringComparison.Ordinal);
            if (end < start) throw new InvalidDataException("SignatureValue inválido no XMLDSIG.");
            return xml.Substring(0, start) + base64Signature + xml.Substring(end);
        }

        private static byte[] CanonicalizeSignedInfo(string signatureXml)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(signatureXml.TrimStart('\uFEFF'));
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
            var signedInfo = doc.SelectSingleNode("/ds:Signature/ds:SignedInfo", ns) as XmlElement;
            if (signedInfo == null) throw new InvalidDataException("SignedInfo não encontrado.");

            var standalone = new XmlDocument { PreserveWhitespace = true };
            var outer = signedInfo.OuterXml;
            if (outer.IndexOf("xmlns=", StringComparison.Ordinal) < 0)
                outer = outer.Replace("<SignedInfo", "<SignedInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\"");
            standalone.LoadXml(outer);

            var transform = new XmlDsigC14NTransform(false);
            transform.LoadInput(standalone);
            using (var canonical = (Stream)transform.GetOutput(typeof(Stream)))
            using (var ms = new MemoryStream())
            {
                canonical.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private static byte[] SignRsaSha1WithCitizenCardPkcs11(X509Certificate2 cert, byte[] canonicalSignedInfo, string modulePath)
        {
            var factories = new Pkcs11InteropFactories();
            using (var library = factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, modulePath, AppType.MultiThreaded))
            {
                var slots = library.GetSlotList(SlotsType.WithTokenPresent);
                if (slots == null || slots.Count == 0) throw new InvalidOperationException("Nenhum Cartão de Cidadão/token PKCS#11 foi detetado.");

                foreach (var slot in slots)
                {
                    using (var session = slot.OpenSession(SessionType.ReadOnly))
                    {
                        byte[] keyId;
                        if (!TryFindCertificateId(session, cert, out keyId)) continue;
                        var privateKey = FindRsaSigningPrivateKey(session, keyId);
                        if (privateKey == null) continue;

                        var tokenInfo = slot.GetTokenInfo();
                        var loggedIn = false;
                        try
                        {
                            if (tokenInfo.TokenFlags.LoginRequired)
                            {
                                if (!tokenInfo.TokenFlags.ProtectedAuthenticationPath)
                                    throw new NotSupportedException("O módulo PKCS#11 não anunciou um caminho de autenticação protegido. O RJP Signer não recolhe nem guarda o PIN do Cartão de Cidadão.");
                                try
                                {
                                    session.Login(CKU.CKU_USER, (byte[])null);
                                    loggedIn = true;
                                }
                                catch (Pkcs11Exception ex)
                                {
                                    if (ex.RV != CKR.CKR_USER_ALREADY_LOGGED_IN) throw;
                                }
                            }

                            using (var mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_SHA1_RSA_PKCS))
                                return session.Sign(mechanism, privateKey, canonicalSignedInfo);
                        }
                        catch (Pkcs11Exception ex)
                        {
                            throw new CryptographicException("Falha PKCS#11 ao assinar em RSA-SHA1: " + ex.RV + ".", ex);
                        }
                        finally
                        {
                            if (loggedIn) { try { session.Logout(); } catch { } }
                        }
                    }
                }
            }
            throw new InvalidOperationException("O certificado selecionado no Windows não foi encontrado no Cartão de Cidadão/token PKCS#11, ou não foi localizada a chave privada RSA correspondente.");
        }

        private static bool TryFindCertificateId(ISession session, X509Certificate2 selectedCertificate, out byte[] keyId)
        {
            keyId = null;
            var template = new List<IObjectAttribute>
            {
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_CERTIFICATE),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CERTIFICATE_TYPE, CKC.CKC_X_509)
            };
            var handles = session.FindAllObjects(template);
            foreach (var handle in handles)
            {
                try
                {
                    var attrs = session.GetAttributeValue(handle, new List<CKA> { CKA.CKA_VALUE, CKA.CKA_ID });
                    if (attrs.Count < 2 || attrs[0].CannotBeRead || attrs[1].CannotBeRead) continue;
                    var raw = attrs[0].GetValueAsByteArray();
                    if (raw == null || raw.Length == 0) continue;
                    bool same;
                    try
                    {
                        using (var tokenCert = new X509Certificate2(raw))
                            same = NormalizeThumbprint(tokenCert.Thumbprint) == NormalizeThumbprint(selectedCertificate.Thumbprint);
                    }
                    catch { same = raw.SequenceEqual(selectedCertificate.RawData); }
                    if (!same) continue;
                    keyId = attrs[1].GetValueAsByteArray();
                    return keyId != null && keyId.Length > 0;
                }
                catch { }
            }
            return false;
        }

        private static IObjectHandle FindRsaSigningPrivateKey(ISession session, byte[] keyId)
        {
            var template = new List<IObjectAttribute>
            {
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_KEY_TYPE, CKK.CKK_RSA),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_ID, keyId),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_SIGN, true)
            };
            return session.FindAllObjects(template).FirstOrDefault();
        }

        private static void VerifyRsaSha1Locally(X509Certificate2 cert, byte[] data, byte[] signature)
        {
            using (var rsa = cert.GetRSAPublicKey())
            {
                if (rsa == null || !rsa.VerifyData(data, signature, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
                    throw new CryptographicException("A assinatura devolvida pelo PKCS#11 não valida com a chave pública do certificado selecionado.");
            }
        }

        private static string ReadSignatureMethod(PackagePart signaturePart)
        {
            if (signaturePart == null) return null;
            using (var stream = signaturePart.GetStream(FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, false))
            {
                var xml = reader.ReadToEnd();
                var marker = "<SignatureMethod Algorithm=\"";
                var start = xml.IndexOf(marker, StringComparison.Ordinal);
                if (start < 0) return null;
                start += marker.Length;
                var end = xml.IndexOf('"', start);
                return end > start ? xml.Substring(start, end - start) : null;
            }
        }

        private static void VerifyDwfx(HttpRequest req, NetworkStream stream, string origin)
        {
            ValidateDwfxRequest(req);
            var filename = SafeFileName(req.Header("X-RJP-Filename"));
            var temp = TempFile(".dwfx");
            File.WriteAllBytes(temp, req.Body);
            try
            {
                using (var package = Package.Open(temp, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var manager = new PackageDigitalSignatureManager(package);
                    if (!manager.IsSigned)
                    {
                        WriteJson(stream, 200, new { ok = true, signed = false, valid = false, verifyResult = "Unsigned", signatureCount = 0, filename = filename }, origin);
                        return;
                    }
                    var result = manager.VerifySignatures(false);
                    var sigs = manager.Signatures.ToList();
                    var first = sigs.FirstOrDefault();
                    var signer = first != null && first.Signer != null ? FriendlySubject(new X509Certificate2(first.Signer)) : "";
                    var certStatus = first != null && first.Signer != null ? PackageDigitalSignatureManager.VerifyCertificate(first.Signer) : X509ChainStatusFlags.NotSignatureValid;
                    var signedParts = sigs.Sum(s => s.SignedParts.Count);
                    WriteJson(stream, 200, new
                    {
                        ok = true, signed = true, valid = result == VerifyResult.Success, verifyResult = result.ToString(),
                        certificateStatus = certStatus.ToString(), signatureCount = sigs.Count, signedParts = signedParts,
                        signer = signer, signedAt = first == null ? null : first.SigningTime.ToString("o"), filename = filename
                    }, origin);
                    Log("Verificado: " + filename + " | OPC=" + result + " | cert=" + certStatus + " | assinaturas=" + sigs.Count);
                }
            }
            finally { try { File.Delete(temp); } catch { } }
        }

        private static void ValidateDwfxRequest(HttpRequest req)
        {
            if (req.Body == null || req.Body.Length < 4) throw new InvalidDataException("DWFx vazio ou inválido.");
            if (req.Body[0] != 0x50 || req.Body[1] != 0x4B) throw new InvalidDataException("O ficheiro não parece ser um DWFx/OPC válido.");
        }

        private static X509Certificate2 FindCertificate(string thumbprint)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                foreach (var cert in store.Certificates.Cast<X509Certificate2>())
                    if (NormalizeThumbprint(cert.Thumbprint) == thumbprint) return new X509Certificate2(cert);
            }
            return null;
        }

        private static bool IsSignatureInfrastructure(Uri uri) { return uri.OriginalString.StartsWith("/package/services/digital-signature/", StringComparison.OrdinalIgnoreCase); }
        private static bool IsRasterOverlayTiff(Uri uri) { var p = uri.OriginalString; return p.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase); }
        private static X509KeyUsageFlags GetKeyUsage(X509Certificate2 cert) { foreach (var ext in cert.Extensions) if (ext is X509KeyUsageExtension) return ((X509KeyUsageExtension)ext).KeyUsages; return X509KeyUsageFlags.None; }
        private static bool LooksLikeCitizenCard(X509Certificate2 cert)
        {
            var s = (cert.Subject + " " + cert.Issuer + " " + cert.FriendlyName).ToUpperInvariant();
            return s.Contains("CARTAO") || s.Contains("CARTÃO") || s.Contains("CITIZEN") || s.Contains("ASSINATURA DIGITAL QUALIFICADA") || s.Contains("AUTENTICACAO.GOV") || s.Contains("AUTENTICAÇÃO.GOV");
        }
        private static string FriendlySubject(X509Certificate2 cert) { var name = cert.GetNameInfo(X509NameType.SimpleName, false); return string.IsNullOrWhiteSpace(name) ? cert.Subject : name; }
        private static string NormalizeThumbprint(string s) { if (string.IsNullOrWhiteSpace(s)) return ""; return new string(s.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant(); }
        private static string SafeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "documento.dwfx";
            name = Uri.UnescapeDataString(name); foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_'); return name;
        }
        private static string BuildOutputName(string name)
        {
            var ext = Path.GetExtension(name);
            var stem = Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".dwfx";
            var suffixes = new[] { "_por assinar", "-por assinar", " por assinar", "_por_assinar", "-por-assinar" };
            foreach (var suffix in suffixes)
                if (stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    stem = stem.Substring(0, stem.Length - suffix.Length).TrimEnd(' ', '_', '-');
                    break;
                }
            if (stem.EndsWith("_ASSINADO", StringComparison.OrdinalIgnoreCase))
                return stem + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ext;
            return stem + "_ASSINADO" + ext;
        }
        private static string BuildDiagnosticSavePath(string requestedSavePath)
        {
            var dir = Path.GetDirectoryName(requestedSavePath);
            if (string.IsNullOrWhiteSpace(dir)) dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var stem = Path.GetFileNameWithoutExtension(requestedSavePath);
            if (stem.EndsWith("_ASSINADO", StringComparison.OrdinalIgnoreCase))
                stem = stem.Substring(0, stem.Length - "_ASSINADO".Length);
            var candidate = Path.Combine(dir, stem + "_ASSINADO_INVALIDO.dwfx");
            var n = 2;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(dir, stem + "_ASSINADO_INVALIDO_" + n + ".dwfx");
                n++;
            }
            return candidate;
        }
        private static string ChooseSavePath(string outputName)
        {
            string selected = null;
            Exception dialogError = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    var initial = Directory.Exists(downloads) ? downloads : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    using (var dialog = new SaveFileDialog
                    {
                        Title = "Guardar DWFx assinado",
                        FileName = outputName,
                        Filter = "DWFx assinado (*.dwfx)|*.dwfx|Todos os ficheiros (*.*)|*.*",
                        FilterIndex = 1,
                        DefaultExt = "dwfx",
                        AddExtension = true,
                        OverwritePrompt = true,
                        CheckPathExists = true,
                        RestoreDirectory = true,
                        InitialDirectory = initial
                    })
                    {
                        if (dialog.ShowDialog() == DialogResult.OK) selected = dialog.FileName;
                    }
                }
                catch (Exception ex) { dialogError = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = false;
            thread.Start();
            thread.Join();
            if (dialogError != null) throw new InvalidOperationException("Não foi possível abrir a janela Guardar como.", dialogError);
            return selected;
        }
        private static string TempFile(string ext) { return Path.Combine(Path.GetTempPath(), "RJP_Signer_" + Guid.NewGuid().ToString("N") + ext); }
        private static string CleanError(Exception ex)
        {
            if (ex is AggregateException && ex.InnerException != null) ex = ex.InnerException;
            if (ex is OperationCanceledException) return ex.Message;
            if (ex.InnerException != null && ex.Message.IndexOf("invocation", StringComparison.OrdinalIgnoreCase) >= 0) ex = ex.InnerException;
            return ex.Message;
        }

        private static void Log(string message)
        {
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + message + Environment.NewLine, Encoding.UTF8); } catch { }
        }

        private static void WriteJson(NetworkStream stream, int status, object value, string origin)
        {
            var bytes = Encoding.UTF8.GetBytes(Json.Serialize(value)); WriteResponse(stream, status, "application/json; charset=utf-8", bytes, origin);
        }

        private static void WriteResponse(NetworkStream stream, int status, string contentType, byte[] body, string origin, Dictionary<string, string> extra = null)
        {
            var reason = status == 200 ? "OK" : status == 204 ? "No Content" : status == 400 ? "Bad Request" : status == 401 ? "Unauthorized" : status == 403 ? "Forbidden" : status == 404 ? "Not Found" : "Internal Server Error";
            var sb = new StringBuilder();
            sb.Append("HTTP/1.1 ").Append(status).Append(' ').Append(reason).Append("\r\n");
            sb.Append("Content-Type: ").Append(contentType).Append("\r\n");
            sb.Append("Content-Length: ").Append(body == null ? 0 : body.Length).Append("\r\n");
            sb.Append("Cache-Control: no-store\r\nConnection: close\r\n");
            if (!string.IsNullOrWhiteSpace(origin) && AllowedOrigins.Contains(origin)) sb.Append("Access-Control-Allow-Origin: ").Append(origin).Append("\r\n");
            sb.Append("Vary: Origin\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\n");
            sb.Append("Access-Control-Allow-Headers: Content-Type, X-RJP-Certificate, X-RJP-Filename, X-RJP-Token, X-RJP-Pair-Code, X-RJP-Sign-Mode\r\n");
            sb.Append("Access-Control-Expose-Headers: X-RJP-Output-Name, X-RJP-Signer, X-RJP-Bridge-Version, X-RJP-Verify-Result, X-RJP-Signed-Parts, X-RJP-Signature-Count, X-RJP-Algorithm, X-RJP-Signed-At, X-RJP-Certificate-Status, X-RJP-Saved, X-RJP-Saved-Name\r\n");
            sb.Append("Access-Control-Allow-Private-Network: true\r\n");
            if (extra != null) foreach (var kv in extra) sb.Append(kv.Key).Append(": ").Append(kv.Value).Append("\r\n");
            sb.Append("\r\n");
            var header = Encoding.ASCII.GetBytes(sb.ToString()); stream.Write(header, 0, header.Length);
            if (body != null && body.Length > 0) stream.Write(body, 0, body.Length); stream.Flush();
        }
    }

    internal sealed class CertificateInfo
    {
        public string thumbprint { get; set; }
        public string subject { get; set; }
        public string issuer { get; set; }
        public string notBefore { get; set; }
        public string notAfter { get; set; }
        public bool valid { get; set; }
        public string keyUsage { get; set; }
        public bool recommended { get; set; }
        public bool citizenCard { get; set; }
    }

    internal sealed class HttpRequest
    {
        public string Method { get; private set; }
        public string Path { get; private set; }
        public Dictionary<string, string> Headers { get; private set; }
        public byte[] Body { get; private set; }
        public string Header(string name) { string v; return Headers.TryGetValue(name, out v) ? v : null; }

        public static HttpRequest Read(NetworkStream stream, int maxBody)
        {
            var headerBytes = ReadUntilHeaderEnd(stream, 64 * 1024); if (headerBytes == null || headerBytes.Length == 0) return null;
            var text = Encoding.ASCII.GetString(headerBytes); var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None); var first = lines[0].Split(' ');
            if (first.Length < 2) throw new InvalidDataException("Pedido HTTP inválido.");
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < lines.Length; i++) { var idx = lines[i].IndexOf(':'); if (idx <= 0) continue; headers[lines[i].Substring(0, idx).Trim()] = lines[i].Substring(idx + 1).Trim(); }
            var len = 0; string cl; if (headers.TryGetValue("Content-Length", out cl) && !int.TryParse(cl, out len)) throw new InvalidDataException("Content-Length inválido.");
            if (len < 0 || len > maxBody) throw new InvalidDataException("Ficheiro demasiado grande para o Bridge.");
            var body = new byte[len]; var off = 0; while (off < len) { var n = stream.Read(body, off, len - off); if (n <= 0) throw new EndOfStreamException("Ligação interrompida durante o envio do ficheiro."); off += n; }
            var rawPath = first[1]; var q = rawPath.IndexOf('?'); if (q >= 0) rawPath = rawPath.Substring(0, q);
            return new HttpRequest { Method = first[0].ToUpperInvariant(), Path = rawPath, Headers = headers, Body = body };
        }

        private static byte[] ReadUntilHeaderEnd(NetworkStream stream, int max)
        {
            using (var ms = new MemoryStream())
            {
                int state = 0; while (ms.Length < max)
                {
                    var b = stream.ReadByte(); if (b < 0) return ms.ToArray(); ms.WriteByte((byte)b);
                    state = state == 0 && b == '\r' ? 1 : state == 1 && b == '\n' ? 2 : state == 2 && b == '\r' ? 3 : state == 3 && b == '\n' ? 4 : b == '\r' ? 1 : 0;
                    if (state == 4) { var a = ms.ToArray(); Array.Resize(ref a, a.Length - 4); return a; }
                }
                throw new InvalidDataException("Cabeçalhos HTTP demasiado grandes.");
            }
        }
    }
}
