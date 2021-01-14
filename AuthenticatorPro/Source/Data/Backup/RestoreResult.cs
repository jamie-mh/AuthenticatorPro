namespace AuthenticatorPro.Data.Backup
{
    internal class RestoreResult
    {
        public readonly int AuthenticatorCount;
        public readonly int CategoryCount;
        public readonly int CustomIconCount;

        public RestoreResult(int authenticatorCount, int categoryCount, int customIconCount)
        {
            AuthenticatorCount = authenticatorCount;
            CategoryCount = categoryCount;
            CustomIconCount = customIconCount;
        }
    }
}