using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class FileListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> FileClick;
        private readonly FileSource _source;
        public override int ItemCount => _source.Count();


        public FileListAdapter(FileSource source)
        {
            _source = source;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = _source.Listing[position];
            var holder = (FileListHolder) viewHolder;

            var iconResource = new int();
            switch(item.Type)
            {
                case FileSource.Type.Up:
                    iconResource = Resource.Drawable.ic_arrow_upward;
                    break;

                case FileSource.Type.Directory:
                    iconResource = Resource.Drawable.ic_folder;
                    break;

                case FileSource.Type.File:
                    iconResource = Resource.Drawable.ic_insert_drive_file;
                    break;

                case FileSource.Type.Backup:
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

            var holder = new FileListHolder(itemView, OnItemClick);

            return holder;
        }

        private void OnItemClick(int position)
        {
            var file = _source.Listing[position];

            if(FileClick != null && (file.Type == FileSource.Type.Backup || file.Type == FileSource.Type.File))
                FileClick.Invoke(this, position);

            if(_source.Navigate(position)) NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return _source.Listing[position].GetHashCode();
        }
    }
}