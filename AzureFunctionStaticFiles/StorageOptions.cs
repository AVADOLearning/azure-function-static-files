namespace AzureFunctionStaticFiles
{
    /// <summary>
    /// Storage configuration options.
    /// </summary>
    public class StorageOptions
    {
        /// <summary>
        /// Storage account connection string.
        /// </summary>
        public string AccountConnectionString { get; set; }

        /// <summary>
        /// Container index filename.
        /// </summary>
        public string IndexName { get; set; }
    }
}
