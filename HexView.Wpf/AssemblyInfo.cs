using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly)]

[assembly: XmlnsDefinition("http://schemas.juknum.com/wpf/hexview", "Juknum.HexView")]
[assembly: XmlnsDefinition("http://schemas.juknum.com/wpf/hexview", "Juknum.HexView.Enums")]
[assembly: XmlnsPrefix("http://schemas.juknum.com/wpf/hexview", "hv")]

[assembly: InternalsVisibleTo("HexView.Wpf.Tests")]
