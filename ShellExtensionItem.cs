namespace ApexDebloater
{
    public class ShellExtensionItem
    {
        public required string Name { get; set; }
        public required string RegistryKeyName { get; set; }
        public required string RegistryPath { get; set; }
        public required string FriendlyDescription { get; set; }
        public bool IsChecked { get; set; }
        public bool IsEnabled { get; set; }
    }
}
