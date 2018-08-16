using System;
using Albireo.Otp;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using ProAuth.Data;
using ProAuth.Utilities;
using SQLite;

namespace ProAuth
{
    class GeneratorAdapter : RecyclerView.Adapter
    {
        private readonly GeneratorSource _genSource;
        private readonly ImplementationSource _implSource;

        public GeneratorAdapter(GeneratorSource genSource, ImplementationSource implSource)
        {
            _genSource = genSource;
            _implSource = implSource;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            Generator gen = _genSource.Get(position + 1);
            Implementation impl = _implSource.Get(gen.ImplementationId);
            GeneratorHolder genHolder = (GeneratorHolder) holder;

            genHolder.Implementation.Text = impl.Name;
            genHolder.Code.Text = gen.Code.ToString();
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