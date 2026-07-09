using System.Windows;

namespace ApexDebloater
{
    public class SoftwareItem
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public bool IsChecked { get; set; }
        
        public bool IsInstalled { get; set; }
        public bool IsNotInstalled => !IsInstalled;
        public Visibility InstalledVisibility => IsInstalled ? Visibility.Visible : Visibility.Collapsed;
    }
}
