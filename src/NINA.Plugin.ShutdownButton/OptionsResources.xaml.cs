using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Plugin.ShutdownButton;

[Export(typeof(ResourceDictionary))]
public partial class OptionsResources : ResourceDictionary {
    public OptionsResources() {
        InitializeComponent();
        this[new Plugin().Name + "_Options"] = this["OptionsTemplate"];
        Remove("OptionsTemplate");
    }
}
