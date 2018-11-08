using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using OtpSharp;
using ProAuth.Data;

namespace ProAuth.Utilities
{
    internal sealed class FilesystemAdapter : RecyclerView.Adapter
    {
        private readonly FilesystemSource _source;

        public FilesystemAdapter(FilesystemSource source)
        {
            _source = source;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            FilesystemSource.Item item = _source.Listing[position];
            FilesystemHolder holder = (FilesystemHolder) viewHolder;

            int iconResource = new int();
            switch(item.Type)
            {
                case FilesystemSource.Type.Up:
                    iconResource = Icons.GetIcon("up");
                    break;

                case FilesystemSource.Type.Directory:
                    iconResource = Icons.GetIcon("folder");
                    break;

                case FilesystemSource.Type.File:
                    iconResource = Icons.GetIcon("file");
                    break;

                case FilesystemSource.Type.Backup:
                    iconResource = Icons.GetIcon("proauth");
                    break;
            }

            holder.Icon.SetImageResource(iconResource);
            holder.Name.Text = item.Name;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.fileListItem, parent, false);

            FilesystemHolder holder = new FilesystemHolder(itemView, OnItemClick);

            return holder;
        }

        public override int ItemCount => _source.Count();

        private void OnItemClick(int position)
        {
            if(_source.Navigate(position))
            {
                NotifyDataSetChanged();
            };
        }
    }
}