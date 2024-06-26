using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

static class Helpers
{
    static readonly SHA1 sha1 = SHA1.Create();

    internal static XmlElement Deserialize(string input)
    {
        using var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(input), XmlDictionaryReaderQuotas.Max);
        XmlDocument xml = new();
        xml.Load(reader);
        return xml["root"];
    }

    internal static string Hash(Stream inputStream) { return ToString(sha1.ComputeHash(inputStream)); }

    internal static string Hash(string path) { return File.Exists(path) ? ToString(sha1.ComputeHash(File.ReadAllBytes(path))) : string.Empty; }

    static string ToString(byte[] value) { return BitConverter.ToString(value).Replace("-", string.Empty); }
}