using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.FilesystemList
{
    internal sealed class FilesystemAdapter : RecyclerView.Adapter
    {
        private readonly FilesystemSource _source;

        public FilesystemAdapter(FilesystemSource source)
        {
            _source = source;
        }

        public override int ItemCount => _source.Count();
        public event EventHandler<int> FileClick;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = _source.Listing[position];
            var holder = (FilesystemHolder) viewHolder;

            var iconResource = new int();
            switch(item.Type)
            {
                case FilesystemSource.Type.Up:
                    iconResource = Resource.Drawable.ic_arrow_upward;
                    break;

                case FilesystemSource.Type.Directory:
                    iconResource = Resource.Drawable.ic_folder;
                    break;

                case FilesystemSource.Type.File:
                    iconResource = Resource.Drawable.ic_insert_drive_file;
                    break;

                case FilesystemSource.Type.Backup:
                    iconResource = Resource.Mipmap.ic_launcher;
                    break;
            }

            holder.Icon.SetImageResource(iconResource);
            holder.Name.Text = item.Name;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.fileListItem, parent, false);

            var holder = new FilesystemHolder(itemView, OnItemClick);

            return holder;
        }

        private void OnItemClick(int position)
        {
            var file = _source.Listing[position];

            if(FileClick != null && (file.Type == FilesystemSource.Type.Backup || file.Type == FilesystemSource.Type.File))
                FileClick.Invoke(this, position);

            if(_source.Navigate(position)) NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return _source.Listing[position].GetHashCode();
        }
    }
}