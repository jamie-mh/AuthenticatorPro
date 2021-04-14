namespace AuthenticatorPro.Droid.Shared.Query
{
    public class WearAuthenticatorCategory
    {
        public readonly string CategoryId;
        public readonly int Ranking;


        public WearAuthenticatorCategory(string categoryId, int ranking)
        {
            CategoryId = categoryId;
            Ranking = ranking;
        }
    }
}