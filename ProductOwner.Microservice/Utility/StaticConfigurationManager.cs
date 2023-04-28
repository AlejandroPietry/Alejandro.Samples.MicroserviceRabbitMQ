namespace ProductOwner.Microservice.Utility
{
    public static class StaticConfigurationManager
    {
        public static IConfiguration AppSetting
        {
            get;
        }

        static StaticConfigurationManager()
        {
            AppSetting = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
        }
    }
}
