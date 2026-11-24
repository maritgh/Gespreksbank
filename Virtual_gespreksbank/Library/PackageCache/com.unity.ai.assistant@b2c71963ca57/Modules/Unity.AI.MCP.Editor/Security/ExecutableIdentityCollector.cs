using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Unity.AI.MCP.Editor.Helpers;
using Unity.AI.MCP.Editor.Models;
using Unity.AI.Toolkit;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.MCP.Editor.Security
{
    /// <summary>
    /// Collects cryptographic identity information for executables.
    /// Includes file hash (SHA256) and code signature information (platform-specific).
    /// Uses caching to avoid expensive re-computation for unchanged executables.
    /// Cache is persisted to SessionState so it survives domain reloads.
    /// </summary>
    static class ExecutableIdentityCollector
    {
        /// <summary>
        /// Cached identity information for executables
        /// </summary>
        class CachedIdentity
        {
            public ExecutableIdentity Identity;
            public DateTime FileModTime;
        }

        /// <summary>
        /// Serializable DTO for persisting cache entries to SessionState.
        /// </summary>
        [Serializable]
        class CachedIdentityEntry
        {
            public string Path;
            public long FileModTimeTicks;
            public string SHA256Hash;
            public DateTime LastModified;
            public bool IsSigned;
            public string SignaturePublisher;
            public string SignatureFriendlyName;
            public string SignatureSubject;
            public bool SignatureValid;
        }

        const string SessionStateKey = "ExecutableIdentityCollector.Cache";

        // Cache keyed by executable path — persisted via SessionState across domain reloads.
        // Invalidated only when the file's modification time changes.
        static readonly Dictionary<string, CachedIdentity> identityCache = new();
        static readonly object cacheLock = new();

        /// <summary>
        /// Restore the identity cache from SessionState on domain reload.
        /// Deferred via EditorTask.delayCall because SessionState is unavailable during
        /// the early [InitializeOnLoadMethod] phase, and the static constructor could
        /// be triggered from the background validation thread.
        /// </summary>
        [InitializeOnLoadMethod]
        static void OnDomainReload()
        {
            EditorTask.delayCall += RestoreCacheFromSessionState;
        }

        /// <summary>
        /// Collect full cryptographic identity for an executable (with caching)
        /// </summary>
        public static ExecutableIdentity CollectIdentity(string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
            {
                return new ExecutableIdentity
                {
                    Path = executablePath,
                    IsSigned = false,
                    SignatureValid = false
                };
            }

            // Check cache first
            DateTime fileModTime = File.GetLastWriteTime(executablePath);
            lock (cacheLock)
            {
                if (identityCache.TryGetValue(executablePath, out var cached))
                {
                    // Cache hit if file hasn't been modified since last computation
                    if (cached.FileModTime == fileModTime)
                    {
                        return cached.Identity;
                    }
                }
            }

            // Cache miss: compute expensive identity
            var identity = new ExecutableIdentity
            {
                Path = executablePath,
                LastModified = fileModTime,
                SHA256Hash = ComputeSHA256(executablePath)
            };

            // Collect signature information (platform-specific)
            CollectSignatureInfo(executablePath, identity);

            // Update cache
            lock (cacheLock)
            {
                identityCache[executablePath] = new CachedIdentity
                {
                    Identity = identity,
                    FileModTime = fileModTime
                };
            }

            // Persist updated cache to SessionState (must run on main thread)
            EditorTask.delayCall += SaveCacheToSessionState;

            return identity;
        }

        /// <summary>
        /// Persist the in-memory cache to SessionState as JSON.
        /// Called on the main thread via EditorTask.delayCall.
        /// </summary>
        static void SaveCacheToSessionState()
        {
            try
            {
                List<CachedIdentityEntry> entries;
                lock (cacheLock)
                {
                    entries = new List<CachedIdentityEntry>(identityCache.Count);
                    foreach (var kvp in identityCache)
                    {
                        var id = kvp.Value.Identity;
                        entries.Add(new CachedIdentityEntry
                        {
                            Path = kvp.Key,
                            FileModTimeTicks = kvp.Value.FileModTime.Ticks,
                            SHA256Hash = id.SHA256Hash,
                            LastModified = id.LastModified,
                            IsSigned = id.IsSigned,
                            SignaturePublisher = id.SignaturePublisher,
                            SignatureFriendlyName = id.SignatureFriendlyName,
                            SignatureSubject = id.SignatureSubject,
                            SignatureValid = id.SignatureValid
                        });
                    }
                }

                var json = JsonConvert.SerializeObject(entries);
                SessionState.SetString(SessionStateKey, json);
            }
            catch (Exception ex)
            {
                McpLog.Warning($"Failed to save identity cache to SessionState: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore cached identities from SessionState after domain reload.
        /// Only entries whose file modification time still matches disk are restored.
        /// </summary>
        static void RestoreCacheFromSessionState()
        {
            try
            {
                var json = SessionState.GetString(SessionStateKey, string.Empty);
                if (string.IsNullOrEmpty(json))
                    return;

                var entries = JsonConvert.DeserializeObject<List<CachedIdentityEntry>>(json);
                if (entries == null)
                    return;

                int restored = 0;
                lock (cacheLock)
                {
                    foreach (var entry in entries)
                    {
                        if (string.IsNullOrEmpty(entry.Path) || !File.Exists(entry.Path))
                            continue;

                        // Validate file hasn't changed since cache was written
                        var currentModTime = File.GetLastWriteTime(entry.Path);
                        if (currentModTime.Ticks != entry.FileModTimeTicks)
                            continue;

                        identityCache[entry.Path] = new CachedIdentity
                        {
                            FileModTime = currentModTime,
                            Identity = new ExecutableIdentity
                            {
                                Path = entry.Path,
                                SHA256Hash = entry.SHA256Hash,
                                LastModified = entry.LastModified,
                                IsSigned = entry.IsSigned,
                                SignaturePublisher = entry.SignaturePublisher,
                                SignatureFriendlyName = entry.SignatureFriendlyName,
                                SignatureSubject = entry.SignatureSubject,
                                SignatureValid = entry.SignatureValid
                            }
                        };
                        restored++;
                    }
                }

                if (restored > 0)
                    McpLog.Log($"Restored {restored} identity cache entries from SessionState");
            }
            catch (Exception ex)
            {
                McpLog.Warning($"Failed to restore identity cache from SessionState: {ex.Message}");
            }
        }

        /// <summary>
        /// Compute SHA256 hash of the executable file
        /// </summary>
        static string ComputeSHA256(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception ex)
            {
                McpLog.Warning($"Failed to compute SHA256 for {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Collect code signature information (platform-specific)
        /// </summary>
        static void CollectSignatureInfo(string executablePath, ExecutableIdentity identity)
        {
            #if UNITY_EDITOR_WIN
            CollectWindowsSignature(executablePath, identity);
            #elif UNITY_EDITOR_OSX
            CollectMacSignature(executablePath, identity);
            #elif UNITY_EDITOR_LINUX
            // Linux doesn't have standard code signing
            identity.IsSigned = false;
            identity.SignatureValid = false;
            #endif
        }

        #if UNITY_EDITOR_WIN
        /// <summary>
        /// Collect Windows Authenticode signature information
        /// </summary>
        private static void CollectWindowsSignature(string executablePath, ExecutableIdentity identity)
        {
            try
            {
                X509Certificate cert = ReadAuthenticodeSigner(executablePath);
                if (cert == null)
                {
                    identity.IsSigned = false;
                    identity.SignatureValid = false;
                    return;
                }

                using (cert)
                {
                    identity.IsSigned = true;
                    identity.SignatureSubject = cert.Subject;

                    // Extract CN (Common Name) as publisher and friendly name
                    if (cert.Subject.Contains("CN="))
                    {
                        int start = cert.Subject.IndexOf("CN=") + 3;
                        int end = cert.Subject.IndexOf(",", start);
                        if (end == -1) end = cert.Subject.Length;
                        string cn = cert.Subject.Substring(start, end - start).Trim();
                        identity.SignaturePublisher = "CN=" + cn;
                        identity.SignatureFriendlyName = cn; // Friendly name without "CN=" prefix
                    }

                    // Validate certificate chain
                    var cert2 = new X509Certificate2(cert);
                    using (cert2)
                    {
                        using var chain = new X509Chain();
                        // RevocationMode is deliberately NoCheck (UUM-142530): an ONLINE
                        // revocation check makes SignatureValid depend on network/OCSP
                        // reachability, so the same executable validates differently across
                        // sessions. That instability flips the identity key between the
                        // "Signed:" and "Hash:" branches (see ExecutableIdentityComparer.
                        // GetIdentityKey), which breaks approval matching and connection
                        // dedup. We only need a stable, deterministic signer identity for
                        // keying — user approval still gates trust — so revocation is not
                        // checked here.
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

                        identity.SignatureValid = chain.Build(cert2);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to collect Windows signature for {executablePath}: {ex.Message}");
                identity.IsSigned = false;
                identity.SignatureValid = false;
            }
        }

        /// <summary>
        /// Returns the signer certificate embedded in an Authenticode-signed file,
        /// or null if the file has no embedded signature. Throws if the file or its
        /// signature can't be read, so a signed file never reads as unsigned.
        /// </summary>
        /// <remarks>
        /// Windows (crypt32) reads the signature, so the file never enters the managed heap.
        /// Don't use X509Certificate.CreateFromSignedFile here (UUM-157911). On Mono it loads the
        /// whole file, then builds a string twice its size: about 2 GB per MCP connection for the
        /// relay and claude.exe, and the Mono GC never gives that memory back. It also returns
        /// the wrong certificate, or throws, for files with valid signatures.
        /// </remarks>
        internal static X509Certificate ReadAuthenticodeSigner(string filePath)
        {
            // CryptQueryObject reports CRYPT_E_NO_MATCH both for an unsigned file and for one it
            // can't open (locked, access denied), so open the file first: if even the most
            // permissive open fails, crypt32 can't read it either, and this throws instead.
            using (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { }

            if (!CryptQueryObject(CERT_QUERY_OBJECT_FILE, filePath, CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED,
                    CERT_QUERY_FORMAT_FLAG_BINARY, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                    out IntPtr store, out IntPtr message, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == CRYPT_E_NO_MATCH)
                    return null;
                throw SignatureReadFailure("CryptQueryObject", error);
            }

            IntPtr signerInfo = IntPtr.Zero;
            IntPtr certContext = IntPtr.Zero;
            try
            {
                int size = 0;
                if (!CryptMsgGetParam(message, CMSG_SIGNER_CERT_INFO_PARAM, 0, IntPtr.Zero, ref size))
                    throw SignatureReadFailure("CryptMsgGetParam", Marshal.GetLastWin32Error());
                signerInfo = Marshal.AllocHGlobal(size);
                if (!CryptMsgGetParam(message, CMSG_SIGNER_CERT_INFO_PARAM, 0, signerInfo, ref size))
                    throw SignatureReadFailure("CryptMsgGetParam", Marshal.GetLastWin32Error());

                certContext = CertFindCertificateInStore(store, X509_PKCS7_ENCODING, 0, CERT_FIND_SUBJECT_CERT, signerInfo, IntPtr.Zero);
                if (certContext == IntPtr.Zero)
                    throw SignatureReadFailure("CertFindCertificateInStore", Marshal.GetLastWin32Error());

                // Copy the DER bytes out: Mono can't build an X509Certificate from a native handle.
                var context = Marshal.PtrToStructure<CERT_CONTEXT>(certContext);
                var der = new byte[context.cbCertEncoded];
                Marshal.Copy(context.pbCertEncoded, der, 0, der.Length);
                return new X509Certificate(der);
            }
            finally
            {
                if (certContext != IntPtr.Zero)
                    CertFreeCertificateContext(certContext);
                if (signerInfo != IntPtr.Zero)
                    Marshal.FreeHGlobal(signerInfo);
                CryptMsgClose(message);
                CertCloseStore(store, 0);
            }
        }

        static CryptographicException SignatureReadFailure(string call, int error) =>
            new CryptographicException($"{call} failed with 0x{error:X8}");

        const int CRYPT_E_NO_MATCH = unchecked((int)0x80092009);
        const int CERT_QUERY_OBJECT_FILE = 1;
        const int CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 1 << 10;
        const int CERT_QUERY_FORMAT_FLAG_BINARY = 1 << 1;
        const int CMSG_SIGNER_CERT_INFO_PARAM = 7;
        const int X509_PKCS7_ENCODING = 0x00010001; // X509_ASN_ENCODING | PKCS_7_ASN_ENCODING
        const int CERT_FIND_SUBJECT_CERT = 11 << 16; // CERT_COMPARE_SUBJECT_CERT << CERT_COMPARE_SHIFT

        [StructLayout(LayoutKind.Sequential)]
        struct CERT_CONTEXT
        {
            public int dwCertEncodingType;
            public IntPtr pbCertEncoded;
            public int cbCertEncoded;
            public IntPtr pCertInfo;
            public IntPtr hCertStore;
        }

        [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CryptQueryObject(int objectType, string obj, int expectedContentTypeFlags,
            int expectedFormatTypeFlags, int flags, IntPtr msgAndCertEncodingType, IntPtr contentType,
            IntPtr formatType, out IntPtr certStore, out IntPtr msg, IntPtr context);

        [DllImport("crypt32.dll", SetLastError = true)]
        static extern bool CryptMsgGetParam(IntPtr msg, int paramType, int index, IntPtr data, ref int dataSize);

        [DllImport("crypt32.dll", SetLastError = true)]
        static extern IntPtr CertFindCertificateInStore(IntPtr certStore, int certEncodingType, int findFlags,
            int findType, IntPtr findPara, IntPtr prevCertContext);

        [DllImport("crypt32.dll")]
        static extern bool CertFreeCertificateContext(IntPtr certContext);

        [DllImport("crypt32.dll")]
        static extern bool CryptMsgClose(IntPtr msg);

        [DllImport("crypt32.dll")]
        static extern bool CertCloseStore(IntPtr certStore, int flags);
        #endif

        #if UNITY_EDITOR_OSX
        /// <summary>
        /// Collect Mac codesign signature information
        /// </summary>
        static void CollectMacSignature(string executablePath, ExecutableIdentity identity)
        {
            try
            {
                // Step 1: Verify signature validity
                var verifyProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/codesign",
                        Arguments = $"--verify --deep --strict \"{executablePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                verifyProcess.Start();
                string verifyOutput = verifyProcess.StandardError.ReadToEnd();
                verifyProcess.WaitForExit();

                identity.IsSigned = verifyProcess.ExitCode == 0;
                identity.SignatureValid = verifyProcess.ExitCode == 0;

                if (!identity.IsSigned)
                {
                    return;
                }

                // Step 2: Extract Team ID and other info
                var displayProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/codesign",
                        Arguments = $"--display --verbose=4 \"{executablePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                displayProcess.Start();
                string displayOutput = displayProcess.StandardError.ReadToEnd();
                displayProcess.WaitForExit();

                // Parse Team ID and Authority (friendly name)
                foreach (var line in displayOutput.Split('\n'))
                {
                    if (line.Contains("TeamIdentifier="))
                    {
                        int start = line.IndexOf("TeamIdentifier=") + 15;
                        identity.SignaturePublisher = line.Substring(start).Trim();
                    }
                    else if (line.Contains("Authority=Developer ID Application:") || line.Contains("Authority=Apple Development:"))
                    {
                        // Extract friendly name from Authority line
                        // Format: "Authority=Developer ID Application: Company Name (TEAMID)"
                        int start = line.IndexOf("Authority=") + 10;
                        string authority = line.Substring(start).Trim();

                        // Remove the prefix (e.g., "Developer ID Application: ")
                        if (authority.Contains(":"))
                        {
                            authority = authority.Substring(authority.IndexOf(":") + 1).Trim();
                        }

                        // Remove the Team ID in parentheses if present
                        if (authority.Contains("(") && authority.Contains(")"))
                        {
                            int parenStart = authority.LastIndexOf("(");
                            authority = authority.Substring(0, parenStart).Trim();
                        }

                        identity.SignatureFriendlyName = authority;
                    }
                }

                identity.SignatureSubject = displayOutput;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to collect Mac signature for {executablePath}: {ex.Message}");
                identity.IsSigned = false;
                identity.SignatureValid = false;
            }
        }
        #endif
    }
}

