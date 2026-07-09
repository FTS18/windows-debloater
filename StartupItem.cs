namespace ApexDebloater
{
    public class StartupItem
    {
        public required string Name { get; set; }
        public required string Command { get; set; }
        public required string RegistryPath { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsChecked { get; set; }
        public string Location { get; set; } = "Registry";
    }
}
