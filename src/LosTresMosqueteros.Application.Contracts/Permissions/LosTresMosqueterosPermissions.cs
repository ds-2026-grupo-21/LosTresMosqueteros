namespace LosTresMosqueteros.Permissions;

public static class LosTresMosqueterosPermissions
{
    public const string GroupName = "LosTresMosqueteros";

    public static class Administration
    {
        private const string Prefix = GroupName + ".Administration";

        public const string Products = Prefix + ".Products";
        public const string Audit = Prefix + ".Audit";
        public const string IntegrationMetrics = Prefix + ".IntegrationMetrics";
    }
}
