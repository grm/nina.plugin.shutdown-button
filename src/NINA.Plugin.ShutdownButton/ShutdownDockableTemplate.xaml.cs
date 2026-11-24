using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Plugin.ShutdownButton;

[Export(typeof(ResourceDictionary))]
public partial class ShutdownDockableTemplate : ResourceDictionary {
    public ShutdownDockableTemplate() { InitializeComponent(); }
}
