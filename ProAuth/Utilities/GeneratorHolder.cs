using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace ProAuth.Utilities
{
    class GeneratorHolder : RecyclerView.ViewHolder
    {
        public int Id { get; set; }
        public TextView IssuerUsername { get; set; }
        public TextView Code { get; set; }
        public TextView Timer { get; set; }

        public GeneratorHolder(View itemView) : base(itemView)
        {
            IssuerUsername = itemView.FindViewById<TextView>(Resource.Id.issuerUsername);
            Code = itemView.FindViewById<TextView>(Resource.Id.code);
            Timer = itemView.FindViewById<TextView>(Resource.Id.timer);
        }
    }
}