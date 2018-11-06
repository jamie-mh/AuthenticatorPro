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

            switch(item.Type)
            {
                case FilesystemSource.Type.Up:
                    holder.Icon.SetImageResource(
                        ThemeHelper.IsDark ? Resource.Drawable.ic_arrow_upward_dark : Resource.Drawable.ic_arrow_upward_light);
                    break;

                case FilesystemSource.Type.Directory:
                    holder.Icon.SetImageResource(
                        ThemeHelper.IsDark ? Resource.Drawable.ic_folder_dark : Resource.Drawable.ic_folder_light);
                    break;

                case FilesystemSource.Type.File:
                    holder.Icon.SetImageResource(
                        ThemeHelper.IsDark ? Resource.Drawable.ic_insert_drive_file_dark : Resource.Drawable.ic_insert_drive_file_light);
                    break;

                case FilesystemSource.Type.Backup:
                    holder.Icon.SetImageResource(Resource.Mipmap.ic_launcher);
                    break;
            }

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