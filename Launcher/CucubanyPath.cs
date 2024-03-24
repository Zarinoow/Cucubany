using CmlLib.Core;

namespace Cucubany.Launcher;

public class CucubanyPath : MinecraftPath
{
    /*
     * Directory structure:
       Root directory : MinecraftPath.BasePath
       - gamedir :
         | - assets : MinecraftPath.Assets
         |    | - indexes
         |    |    | - {asset id}.json : MinecraftPath.GetIndexFilePath(assetId)
         |    | - objects : MinecraftPath.GetAssetObjectPath(assetId)
         |    | - virtual
         |         | - legacy : MinecraftPath.GetAssetLegacyPath(assetId)
         |
         | - libraries : MinecraftPath.Library
         | - resources : MinecraftPath.Resource
         | - runtime : MinecraftPath.Runtime
         | - versions : MinecraftPath.Versions
              | - {version name}
                    | - {version name}.jar : MinecraftPath.GetVersionJarPath("version_name")
                    | - {version name}.json : MinecraftPath.GetVersionJsonPath("version_name")
                    | - natives : MinecraftPath.GetNativePath("version_name")
     */
    public CucubanyPath()
    {
        string path = GetOSDefaultPath();
        path = path.Substring(0, path.Length - 9);
        path += "cucubany";
        
        BasePath = NormalizePath(path);
        
        Library = NormalizePath(this.BasePath + "/gamedir/libraries");
        Versions = NormalizePath(this.BasePath + "/gamedir/versions");
        Resource = NormalizePath(this.BasePath + "/gamedir/resources");
        Runtime = NormalizePath(this.BasePath + "/gamedir/runtime");
        Assets = NormalizePath(this.BasePath + "/gamedir/assets");
        
        CreateDirs();
    }
    
}