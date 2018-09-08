using System;
using Android.Support.V7.Widget;
using Android.Views;
using ProAuth.Data;

namespace ProAuth.Utilities
{
    internal sealed class GeneratorAdapter : RecyclerView.Adapter
    {
        private readonly GeneratorSource _genSource;
        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemOptionsClick;

        public GeneratorAdapter(GeneratorSource genSource)
        {
            _genSource = genSource;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            Generator gen = _genSource.GetNth(position);
            GeneratorHolder genHolder = (GeneratorHolder) holder;

            genHolder.Issuer.Text = gen.Issuer;
            genHolder.Username.Text = gen.Username;

            if(gen.Username == "")
            {
                genHolder.Username.Visibility = ViewStates.Gone;
            }

            string codePadded = gen.Code;
            int spacesInserted = 0;
            int length = codePadded.Length;

            for(int i = 0; i < length; ++i)
            {
                if(i % 3 == 0 && i > 0)
                {
                    codePadded = codePadded.Insert(i + spacesInserted, " ");
                    spacesInserted++;
                }
            }

            genHolder.Code.Text = codePadded;
            genHolder.Timer.Text = (gen.TimeRenew - DateTime.Now).Seconds.ToString();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.generator_item, parent, false);
            GeneratorHolder holder = new GeneratorHolder(itemView, OnItemClick, this.OnItemOptionsClick);

            return holder;
        }

        public override int ItemCount => _genSource.Count();

        private void OnItemClick(int e)
        {
            ItemClick?.Invoke(this, e);
        }

        private void OnItemOptionsClick(int e)
        {
            ItemOptionsClick?.Invoke(this, e);
        }
    }
}