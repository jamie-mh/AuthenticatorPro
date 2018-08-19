using System;
using Android.Support.V7.Widget;
using Android.Views;
using ProAuth.Data;

namespace ProAuth.Utilities
{
    class GeneratorAdapter : RecyclerView.Adapter
    {
        private readonly GeneratorSource _genSource;

        public GeneratorAdapter(GeneratorSource genSource)
        {
            _genSource = genSource;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            Generator gen = _genSource.GetNth(position);
            GeneratorHolder genHolder = (GeneratorHolder) holder;

            string issuerUsername = gen.Issuer;

            if(gen.Username != "")
            {
                issuerUsername += $" - {gen.Username}";
            }

            genHolder.IssuerUsername.Text = issuerUsername;

            genHolder.Code.Text = gen.Code;
            genHolder.Timer.Text = (gen.TimeRenew - DateTime.Now).Seconds.ToString();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.generator_item, parent, false);
            GeneratorHolder holder = new GeneratorHolder(itemView);

            return holder;
        }

        public override int ItemCount => _genSource.Count();
    }
}