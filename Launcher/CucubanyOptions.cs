// Fichier: Launcher/CucubanyOptions.cs
using System;
using System.IO;
using System.Collections.Generic;
using CmlLib.Core;
using Newtonsoft.Json;

namespace Cucubany.Launcher
{
    public class CucubanyOptions : MLaunchOption
    {
        private CucubanyPath Path;
        private bool useCustomJavaPath = false;
        public string CustomJavaPath { get; set; }
        public string LastConnectedAccount { get; set; }

        public CucubanyOptions(CucubanyPath path)
        {
            Path = path;
            Load();
            VersionType = "Cucubany";
            GameLauncherName = "Cucubany";
            GameLauncherVersion = "2";
            RebuildJvmArgs();
        }

        private void RebuildJvmArgs()
        {
            var args = new List<string>
            {
                $"-Xms{MinimumRamMb}M",
                $"-Xmx{MaximumRamMb}M",
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:+UseG1GC",
                "-XX:G1NewSizePercent=20",
                "-XX:G1ReservePercent=20",
                "-XX:MaxGCPauseMillis=50",
                "-XX:G1HeapRegionSize=16M",
                "-Djava.net.preferIPv4Stack=true"
            };
            JVMArguments = args.ToArray();
        }

        public void SetMemory(int minMb, int maxMb)
        {
            MinimumRamMb = minMb;
            MaximumRamMb = maxMb;
            RebuildJvmArgs();
            Save();
        }

        public void Save()
        {
            string path = Path.BasePath + "/launcher_settings.json";
            if (File.Exists(path)) File.Delete(path);
            using var file = File.AppendText(path);
            using var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented };
            writer.WriteStartObject();
            writer.WritePropertyName("MinimumRamMb"); writer.WriteValue(MinimumRamMb);
            writer.WritePropertyName("MaximumRamMb"); writer.WriteValue(MaximumRamMb);
            writer.WritePropertyName("ScreenSize");
            writer.WriteStartObject();
            writer.WritePropertyName("ScreenWidth"); writer.WriteValue(ScreenWidth);
            writer.WritePropertyName("ScreenHeight"); writer.WriteValue(ScreenHeight);
            writer.WritePropertyName("FullScreen"); writer.WriteValue(FullScreen);
            writer.WriteEndObject();
            writer.WritePropertyName("CustomJavaPath");
            writer.WriteStartObject();
            writer.WritePropertyName("UseCustomJavaPath"); writer.WriteValue(useCustomJavaPath);
            writer.WritePropertyName("JavaPath"); writer.WriteValue(CustomJavaPath);
            writer.WriteEndObject();
            writer.WritePropertyName("lastConnectedAccount"); writer.WriteValue(LastConnectedAccount);
            if (MainWindow.KonamiCodeEnabled)
            {
                writer.WritePropertyName("KonamiCodeEnabled");
                writer.WriteValue(true);
            }
            writer.WriteEndObject();
        }

        public void Load()
        {
            string path = Path.BasePath + "/launcher_settings.json";
            if (!File.Exists(path))
            {
                MinimumRamMb = 512;
                MaximumRamMb = 4096;
                ScreenWidth = 854;
                ScreenHeight = 480;
                FullScreen = false;
                Save();
            }
            else
            {
                using var file = File.OpenText(path);
                using var reader = new JsonTextReader(file);
                while (reader.Read())
                {
                    if (reader.TokenType != JsonToken.PropertyName) continue;
                    string name = reader.Value.ToString();
                    reader.Read();
                    switch (name)
                    {
                        case "MinimumRamMb":
                            MinimumRamMb = Convert.ToInt32(reader.Value);
                            break;
                        case "MaximumRamMb":
                            MaximumRamMb = Convert.ToInt32(reader.Value);
                            break;
                        case "ScreenSize":
                            reader.Read();
                            while (reader.TokenType != JsonToken.EndObject)
                            {
                                string sn = reader.Value.ToString();
                                reader.Read();
                                switch (sn)
                                {
                                    case "ScreenWidth": ScreenWidth = Convert.ToInt32(reader.Value); break;
                                    case "ScreenHeight": ScreenHeight = Convert.ToInt32(reader.Value); break;
                                    case "FullScreen": FullScreen = Convert.ToBoolean(reader.Value); break;
                                }
                                reader.Read();
                            }
                            break;
                        case "CustomJavaPath":
                            reader.Read();
                            while (reader.TokenType != JsonToken.EndObject)
                            {
                                string jn = reader.Value.ToString();
                                reader.Read();
                                switch (jn)
                                {
                                    case "UseCustomJavaPath": useCustomJavaPath = Convert.ToBoolean(reader.Value); break;
                                    case "JavaPath": CustomJavaPath = reader.Value?.ToString() ?? string.Empty; break;
                                }
                                reader.Read();
                            }
                            if (useCustomJavaPath) JavaPath = CustomJavaPath;
                            break;
                        case "lastConnectedAccount":
                            LastConnectedAccount = reader.Value?.ToString() ?? string.Empty;
                            break;
                        case "KonamiCodeEnabled":
                            MainWindow.KonamiCodeEnabled = true;
                            break;
                    }
                }
            }
            RebuildJvmArgs();
        }

        public void EnableCustomJavaPath(bool enable)
        {
            useCustomJavaPath = enable;
            JavaPath = enable ? CustomJavaPath : null;
        }

        public bool IsCustomJavaPathEnabled() => useCustomJavaPath;

        public void UpdateSession()
        {
            Session = LauncherMain.GetInstance().GetSession();
        }
    }
}