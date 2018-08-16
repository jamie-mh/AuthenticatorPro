using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace ProAuth
{
    class GeneratorHolder : RecyclerView.ViewHolder
    {
        public int Id { get; set; }
        public TextView Implementation { get; set; }
        public TextView Code { get; set; }
        public TextView Timer { get; set; }

        public GeneratorHolder(View itemView) : base(itemView)
        {
            Implementation = itemView.FindViewById<TextView>(Resource.Id.implementation);
            Code = itemView.FindViewById<TextView>(Resource.Id.code);
            Timer = itemView.FindViewById<TextView>(Resource.Id.timer);
        }
    }
}