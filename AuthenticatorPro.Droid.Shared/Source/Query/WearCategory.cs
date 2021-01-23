namespace AuthenticatorPro.Droid.Shared.Query
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class WearCategory
    {
        public readonly string Id;
        public readonly string Name;

        
        public WearCategory(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}