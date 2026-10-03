namespace AnimalClassifier.RateLimiting
{
    /// <summary>
    /// The names an endpoint opts into a limit by, with
    /// <c>[EnableRateLimiting]</c>.
    /// </summary>
    public static class RateLimitPolicies
    {
        public const string Login = "LoginPolicy";
        public const string PasswordReset = "PasswordResetPolicy";
        public const string DataExport = "DataExportPolicy";
    }
}
