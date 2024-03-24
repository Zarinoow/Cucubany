namespace Cucubany.Launcher;

public class Mod
{
    public Mod(string name, string downloadUrl, string expectedHash, string fileName)
    {
        Name = name;
        DownloadUrl = downloadUrl;
        ExpectedHash = expectedHash;
        FileName = fileName;
    }

    public string Name { get; }
    public string DownloadUrl { get; }
    public string ExpectedHash { get;}
    public string FileName { get;}
}