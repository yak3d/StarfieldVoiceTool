using System.Reflection;
using StarfieldVT.Core.Models;

namespace StarfieldVT.Core;

public class CacheFile
{
    public List<Master> Masters { get; set; } = new List<Master>();
    public Version Version { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? Version.Parse("0.0.0.0");
}