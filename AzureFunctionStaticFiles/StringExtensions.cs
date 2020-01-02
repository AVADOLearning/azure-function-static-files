namespace AzureFunctionStaticFiles
{
    /// <summary>
    /// String extension methods.
    /// </summary>
    static class StringExtensions
    {
        /// <summary>
        /// Return the value or a default value if null or empty.
        /// </summary>
        /// <param name="s">
        /// The string value (this).
        /// </param>
        /// <param name="defaultValue">
        /// The fallback value.
        /// </param>
        public static string ValueOrDefault(this string s, string defaultValue)
        {
            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }
            return s;
        }
    }
}
