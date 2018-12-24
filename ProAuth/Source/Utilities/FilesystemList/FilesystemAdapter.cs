using System;
using System.Text.RegularExpressions;
using Android.Views;

namespace ProAuth.Utilities.FilesystemList
{
    internal sealed class FilesystemAdapter : Android.Support.V7.Widget.RecyclerView.Adapter
    {
        private readonly FilesystemSource _source;
        public event EventHandler<int> BackupClick;

        public FilesystemAdapter(FilesystemSource source)
        {
            _source = source;
        }

        public override void OnBindViewHolder(Android.Support.V7.Widget.RecyclerView.ViewHolder viewHolder, int position)
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

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.fileListItem, parent, false);

            FilesystemHolder holder = new FilesystemHolder(itemView, OnItemClick);

            return holder;
        }

        public override int ItemCount => _source.Count();

        private void OnItemClick(int position)
        {
            FilesystemSource.Item file = _source.Listing[position];

            if(BackupClick != null && file.Type == FilesystemSource.Type.Backup)
            {
                BackupClick.Invoke(this, position);
            }

            if(_source.Navigate(position))
            {
                NotifyDataSetChanged();
            };
        }

        public override long GetItemId(int position)
        {
            return _source.Listing[position].GetHashCode();
        }
    }
}