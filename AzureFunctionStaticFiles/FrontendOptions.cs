namespace AzureFunctionStaticFiles
{
    /// <summary>
    /// Frontend configuration options.
    /// </summary>
    public class FrontendOptions
    {
        /// <summary>
        /// Host name of the site.
        /// </summary>
        /// <remarks>
        /// Defaults to the value of the <code>Host</code> header in the request.
        /// </remarks>
        public string HostName { get; set; }
    }
}
