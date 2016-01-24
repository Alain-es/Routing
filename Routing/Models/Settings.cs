namespace Routing.Models
{
    public class Settings
    {
        public string RoutesExamineSearchProvider { get; set; }

        public bool RoutesAreCaseSensitive { get; set; }
        public bool RoutesAreAccentSensitive { get; set; }

        public int CacheDurationInHours { get; set; }

        public string PersistentCacheMapPath { get; set; }
        public int PersistentCacheUpdateFrequencyInMinutes { get; set; }

        public Settings()
        {
            RoutesExamineSearchProvider = "ExternalSearcher";
            RoutesAreCaseSensitive = false;
            RoutesAreAccentSensitive = true;
            CacheDurationInHours = 24;
            PersistentCacheMapPath = "~/App_Data/TEMP/Routing/persistentCache.dat";
            PersistentCacheUpdateFrequencyInMinutes = 10;
        }
    }
}