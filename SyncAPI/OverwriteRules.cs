namespace MachinationsUP.SyncAPI
{

    /// <summary>
    /// Defines how an ElementBase is transacted with the Machinations Diagram.
    /// </summary>
    public enum OverwriteRules : int
    {
        /// <summary>
        /// Accept the value from Machinations Diagram.
        /// </summary>
        Accept,

        /// <summary>
        /// Ignore the value.
        /// </summary>
        Ignore,

        /// <summary>
        /// Pick the newer value.
        /// </summary>
        Sync
    }
}
