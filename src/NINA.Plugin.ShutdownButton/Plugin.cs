using System.ComponentModel.Composition;
using NINA.Plugin;
using NINA.Plugin.Interfaces;

namespace NINA.Plugin.ShutdownButton;

[Export(typeof(IPluginManifest))]
public sealed class Plugin : PluginBase { }
