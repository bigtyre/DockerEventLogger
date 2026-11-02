namespace BigTyre.Docker.Configuration
{
    public class AppSettings
    {
        public string? MySqlConnectionString { get; set; }

        /// <summary>Configured retention period in days; cleanup is not implemented yet.</summary>
        public int RetentionDays { get; set; } = 90;
    }
}

