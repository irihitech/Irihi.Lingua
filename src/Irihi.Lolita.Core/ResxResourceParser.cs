using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml;

namespace Irihi.Lolita.Core;

public sealed class ResxResource
{
    public string Name { get; }
    public IReadOnlyList<string> Keys { get; }

    public ResxResource(string name, IReadOnlyList<string> keys)
    {
        Name = name;
        Keys = keys;
    }
}

public static class ResxResourceParser
{
    public static ResxResource Parse(string filePath, string xml)
    {
        var resourceName = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            var document = XDocument.Parse(xml);
            var keys = document
                .Root?
                .Elements("data")
                .Select(x => x.Attribute("name")?.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .Cast<string>()
                .ToArray() ?? Array.Empty<string>();

            return new ResxResource(resourceName, keys);
        }
        catch (XmlException)
        {
            return new ResxResource(resourceName, Array.Empty<string>());
        }
    }
}
