﻿using System;
using System.IO;
using CmlLib.Core;
using Newtonsoft.Json;

namespace Cucubany.Launcher;

public class CucubanyOptions : MLaunchOption
{
    /*
     * Options: 
     * RAM (MaximumRamMb, MinimumRamMb) ✅
     * JavaPath (JavaPath) ✅
     * ScreenSize (ScreenWidth, ScreenHeight, FullScreen) ✅
     * Session (Session) ✅
     */
    private CucubanyPath Path;
    
    /*
     * Customs variables:
     */
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
        JVMArguments = new[] { 
            "-XX:+UnlockExperimentalVMOptions",
            "-XX:+UseG1GC",
            "-XX:G1NewSizePercent=20",
            "-XX:G1ReservePercent=20",
            "-XX:MaxGCPauseMillis=50",
            "-XX:G1HeapRegionSize=16M",
            "-Djava.net.preferIPv4Stack=true"
        };
    }
    
    /*
     * Data
     */
    
    public void Save()
    {
        String path = Path.BasePath + "/launcher_settings.json";
        
        if(File.Exists(path)) File.Delete(path);
        
        using (StreamWriter file = File.AppendText(path))
        using (JsonTextWriter writer = new JsonTextWriter(file))
        {
            writer.Formatting = Formatting.Indented;

            // Start writing
            writer.WriteStartObject();

            // Writing RAM settings
            writer.WritePropertyName("MinimumRamMb");
            writer.WriteValue(MinimumRamMb);

            writer.WritePropertyName("MaximumRamMb");
            writer.WriteValue(MaximumRamMb);
            
            // Writing Screen settings
            writer.WritePropertyName("ScreenSize");
            writer.WriteStartObject();
           
            writer.WritePropertyName("ScreenWidth");
            writer.WriteValue(ScreenWidth);
            
            writer.WritePropertyName("ScreenHeight");
            writer.WriteValue(ScreenHeight);
            
            writer.WritePropertyName("FullScreen");
            writer.WriteValue(FullScreen);
            
            writer.WriteEndObject();
            
            // Writing Java path
            writer.WritePropertyName("CustomJavaPath");
            writer.WriteStartObject();
            
            writer.WritePropertyName("UseCustomJavaPath");
            writer.WriteValue(useCustomJavaPath);
            
            writer.WritePropertyName("JavaPath");
            writer.WriteValue(CustomJavaPath);
            
            writer.WriteEndObject();
            
            // Last session
            writer.WritePropertyName("lastConnectedAccount");
            writer.WriteValue(LastConnectedAccount);
            
            if(MainWindow.KonamiCodeEnabled)
            {
                writer.WritePropertyName("KonamiCodeEnabled");
                writer.WriteValue(MainWindow.KonamiCodeEnabled);
            }
            
            // End writing
            writer.WriteEndObject();
        }
    }
    
    public void Load()
    {
        String path = Path.BasePath + "/launcher_settings.json";

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
            using (StreamReader file = File.OpenText(path))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            String name = reader.Value.ToString();
                            reader.Read();
                            
                            switch (name)
                            {
                                case "MinimumRamMb":
                                    MinimumRamMb = int.Parse(reader.Value.ToString());
                                    break;
                                case "MaximumRamMb":
                                    MaximumRamMb = int.Parse(reader.Value.ToString());
                                    break;
                                case "ScreenSize":
                                    reader.Read();
                                    while (reader.TokenType != JsonToken.EndObject)
                                    {
                                        String screenName = reader.Value.ToString();
                                        reader.Read();
                                        
                                        switch (screenName)
                                        {
                                            case "ScreenWidth":
                                                ScreenWidth = int.Parse(reader.Value.ToString());
                                                break;
                                            case "ScreenHeight":
                                                ScreenHeight = int.Parse(reader.Value.ToString());
                                                break;
                                            case "FullScreen":
                                                FullScreen = bool.Parse(reader.Value.ToString());
                                                break;
                                        }
                                        reader.Read();
                                    }
                                    break;
                                case "CustomJavaPath":
                                    reader.Read();
                                    while (reader.TokenType != JsonToken.EndObject)
                                    {
                                        String javaName = reader.Value.ToString();
                                        reader.Read();
                                        
                                        switch (javaName)
                                        {
                                            case "UseCustomJavaPath":
                                                useCustomJavaPath = bool.Parse(reader.Value.ToString());
                                                break;
                                            case "JavaPath":
                                                if(reader.Value == null) CustomJavaPath = string.Empty;
                                                else CustomJavaPath = reader.Value.ToString();
                                                break;
                                        }
                                        reader.Read();
                                    }
                                    break;
                                case "lastConnectedAccount":
                                    if(reader.Value == null) LastConnectedAccount = string.Empty;
                                    else LastConnectedAccount = reader.Value.ToString();
                                    break;
                                case "KonamiCodeEnabled":
                                    MainWindow.KonamiCodeEnabled = true;
                                    break;
                            }
                        }
                    }
                }
            }
        }
        
    }
    
    /*
     * Java path
     */
    
    public void EnableCustomJavaPath(bool enable)
    {
        useCustomJavaPath = enable;
        if (enable)
        {
            JavaPath = CustomJavaPath;
        }
        else
        {
            JavaPath = null;
        }
    }
    
    public bool IsCustomJavaPathEnabled()
    {
        return useCustomJavaPath;
    }
    
    /*
     * Session management
     */
    public void UpdateSession()
    {
        Session = LauncherMain.GetInstance().GetSession();
    }
    
}