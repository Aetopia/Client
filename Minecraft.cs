using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;

interface IArtifact
{
    string Path { get; }

    string SHA1 { get; }

    string Url { get; }

    bool Natives { get; }
}

file class Artifact(string path, string sha1, string url, bool natives = false) : IArtifact
{
    public string Path => path;

    public string SHA1 => sha1;

    public string Url => url;

    public bool Natives => natives;
}

static class Minecraft
{
    static readonly WebClient client = new();

    static XmlElement Deserialize(string address) { return Helpers.Deserialize(client.DownloadString(address)); }

    static readonly string architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

    static readonly string value = $"natives-windows-{architecture.TrimStart('x')}";

    static bool CompareHash(Stream inputStream, string path)
    {
        return Helpers.Hash(inputStream).Equals(Helpers.Hash(path));
    }

    internal static void Launch(Dictionary<string, IArtifact>.ValueCollection artifacts)
    {
        Directory.CreateDirectory("natives");
        foreach (var artifact in artifacts)
        {
            Console.WriteLine(artifact.Natives);
            // using var zip = ZipFile.OpenRead(artifact.Path);
            //    foreach (var entry in zip.Entries)
            //   {
            //       Console.WriteLine(entry.Name);
            //  var path = Path.Combine("natives", entry.Name);
            //  if (!entry.Name.EndsWith(".dll", StringComparison.Ordinal)) continue;
            //  using var inputStream = entry.Open();
            //  if (CompareHash(inputStream, path)) continue;
            //   entry.ExtractToFile(path, true);
            //  }
        }
    }

    internal static Dictionary<string, IArtifact>.ValueCollection Get()
    {
        var manifest = Deserialize(
            Deserialize("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json")
            .GetElementsByTagName("id")
            .Cast<XmlElement>()
            .FirstOrDefault(element => element.InnerText.Equals("1.8.9"))
            .ParentNode["url"].InnerText
        );

        Dictionary<string, IArtifact> artifacts = [];

        artifacts.GetLibraries(manifest);

        artifacts.GetAssets(manifest["assetIndex"]["url"].InnerText);

        artifacts.GetJRE();

        artifacts.Add("1.8.json", new Artifact(
            @"assets\indexes\1.8.json",
            manifest["assetIndex"]["sha1"].InnerText,
            manifest["assetIndex"]["url"].InnerText));

        artifacts.Add("client.jar", new Artifact(
            @"jars\client.jar",
            manifest["downloads"]["client"]["sha1"].InnerText,
            manifest["downloads"]["client"]["url"].InnerText));

        return artifacts.Values;
    }

    static void GetAssets(this Dictionary<string, IArtifact> artifacts, string address)
    {
        foreach (XmlElement hash in Deserialize(address).GetElementsByTagName("hash"))
            if (!artifacts.ContainsKey(hash.InnerText))
            {
                var path = $"{hash.InnerText.Substring(0, 2)}/{hash.InnerText}";
                artifacts.Add(hash.InnerText, new Artifact(
                    Path.Combine(@"assets\objects", path),
                    hash.InnerText,
                    $"https://resources.download.minecraft.net/{path}"
                ));
            }
    }

    static void GetLibraries(this Dictionary<string, IArtifact> artifacts, XmlElement manifest)
    {
        foreach (XmlElement library in manifest["libraries"].GetElementsByTagName("downloads"))
        {
            var name = library.ParentNode["name"].InnerText;
            var key = name.Substring(0, name.LastIndexOf(':'));
            if (artifacts.ContainsKey(key)) return;

            Console.WriteLine(key);

            var natives = false;
            var artifact = library.FirstChild;
            if (natives = library.LastChild.Name.Equals("classifiers"))
            {
                artifact = library["classifiers"]
                .Cast<XmlElement>()
                .First(element => element.Name.Equals("natives-windows") || element.Name.Equals(value));
                Console.WriteLine(artifact["path"].InnerText);
            }

            artifacts.Add(key, new Artifact(
                Path.Combine("jars", Path.GetFileName(artifact["path"].InnerText)),
                artifact["sha1"].InnerText,
                artifact["url"].InnerText,
                natives
            ));
        }
    }

    internal static void GetJRE(this Dictionary<string, IArtifact> artifacts)
    {
        foreach (var raw in Deserialize(
            ((XmlElement)((XmlElement)Deserialize("https://launchermeta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json")
            .GetElementsByTagName($"windows-{architecture}")[0])
            .GetElementsByTagName("jre-legacy")[0])
            .GetElementsByTagName("url")[0].InnerText)
            .GetElementsByTagName("raw")
            .Cast<XmlElement>())
        {
            var file = raw.ParentNode.ParentNode;
            var key = file.Attributes["item"]?.Value;
            if (artifacts.ContainsKey(key ??= file.Name)) continue;

            artifacts.Add(key, new Artifact(
                Path.Combine("jre", key),
                raw["sha1"].InnerText,
                raw["url"].InnerText
            ));
        }
    }
}