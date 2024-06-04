namespace codecrafters_http_server.src.Helper
{
    /// <summary>
    /// Helper class containing general-purpose methods
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// This is a basic method that returns the result of splitting a string
        /// </summary>
        public static string[] SplitString(string str, char symbol)
        {
            return str.Trim().Split(symbol);
        }
    }
}