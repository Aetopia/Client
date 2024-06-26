using System;
using System.IO;
using System.Net;

static class Program
{
    static void Main()
    {
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        WebClient client = new();
        var artifacts = Minecraft.Get();
        
        foreach (var artifact in artifacts)
        {
            if (CompareHash(artifact)) continue;
            Console.WriteLine($"[DOWNLOAD] {artifact.Path}");
            Directory.CreateDirectory(Path.GetDirectoryName(artifact.Path));
            client.DownloadFile(artifact.Url, artifact.Path);
        }

      //  Minecraft.Launch(artifacts);
    }

    static bool CompareHash(IArtifact artifact) { return Helpers.Hash(artifact.Path).Equals(artifact.SHA1, StringComparison.OrdinalIgnoreCase); }
}