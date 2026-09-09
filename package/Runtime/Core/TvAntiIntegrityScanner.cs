using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TvAnti
{
    public sealed class TvAntiIntegrityScanner
    {
        private readonly TvAntiConfig config;
        private readonly HashSet<string> reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TvAntiIntegrityScanner(TvAntiConfig config) => this.config = config;

        public IEnumerable<TvAntiEvent> ScanLoadedAssemblies()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                string assemblyName = assembly.GetName().Name ?? string.Empty;
                string fullName = assembly.FullName ?? assemblyName;

                foreach (var rule in TvAntiThreatCatalog.Rules)
                {
                    if (!MatchesExactAssembly(assemblyName, rule))
                        continue;

                    string key = "asm:" + assemblyName + ":" + rule.Id;
                    if (reported.Add(key))
                    {
                        yield return new TvAntiEvent(
                            TvAntiViolation.UnexpectedAssembly,
                            1f,
                            "High-confidence exact assembly match: " + fullName + " [" + rule.Id + "]");
                    }
                }
            }
        }

        public IEnumerable<TvAntiEvent> ScanKnownPluginLocations()
        {
            var roots = new[]
            {
                Application.dataPath,
                Application.persistentDataPath,
                Application.streamingAssetsPath
            };

            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                    continue;

                foreach (var rule in TvAntiThreatCatalog.Rules)
                {
                    foreach (string fileName in rule.ExactFileNames)
                    {
                        string[] matches;
                        try
                        {
                            matches = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
                        }
                        catch
                        {
                            continue;
                        }

                        foreach (string path in matches)
                        {
                            string key = "file:" + path;
                            if (!reported.Add(key))
                                continue;

                            yield return new TvAntiEvent(
                                TvAntiViolation.UnexpectedNativeModule,
                                1f,
                                "High-confidence exact plugin filename match: " + path + " [" + rule.Id + "]");
                        }
                    }
                }
            }
        }

        private static bool MatchesExactAssembly(string assemblyName, TvAntiThreatRule rule)
        {
            foreach (string exact in rule.ExactAssemblyNames)
            {
                if (string.Equals(assemblyName, exact, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
