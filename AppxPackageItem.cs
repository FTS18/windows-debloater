namespace ApexDebloater
{
    public class AppxPackageItem
    {
        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required string Version { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsChecked { get; set; }
        public string SizeMb { get; set; } = "Calculating...";
        public string? UninstallString { get; set; }
        public bool IsDesktopApp { get; set; }
    }
}
