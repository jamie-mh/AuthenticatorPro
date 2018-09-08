using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace ProAuth.Utilities
{
    class GeneratorHolder : RecyclerView.ViewHolder
    {
        public int Id { get; set; }
        public TextView Issuer { get; set; }
        public TextView Username { get; }
        public TextView Code { get; set; }
        public TextView Timer { get; set; }

        public GeneratorHolder(View itemView, Action<int> clickListener, Action<int> optionsClickListener) : base(itemView)
        {
            Issuer = itemView.FindViewById<TextView>(Resource.Id.issuer);
            Username = itemView.FindViewById<TextView>(Resource.Id.username);
            Code = itemView.FindViewById<TextView>(Resource.Id.code);
            Timer = itemView.FindViewById<TextView>(Resource.Id.timer);

            ImageButton optionsButton = itemView.FindViewById<ImageButton>(Resource.Id.options);

            itemView.Click += (sender, e) => clickListener(AdapterPosition);
            optionsButton.Click += (sender, e) => optionsClickListener(AdapterPosition);
        }
    }
}