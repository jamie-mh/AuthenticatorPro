namespace AuthenticatorPro.Shared.Query
{
    public class WearCategoryResponse
    {
        public readonly string Id;
        public readonly string Name;

        
        public WearCategoryResponse(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}