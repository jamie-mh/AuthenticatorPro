using System.Collections.Generic;
using System.IO;

namespace AuthenticatorPro.Data
{
    internal class FileSource
    {
        private readonly string _root;
        public bool CanNavigateUp;

        public FileSource(string root)
        {
            _root = root;
            CurrentPath = root;
            CanNavigateUp = false;

            Listing = new List<Item>();
            Update();
        }

        public string CurrentPath { get; private set; }
        public List<Item> Listing { get; }

        public void Update()
        {
            Listing.Clear();

            if(CurrentPath != _root)
            {
                Listing.Add(new Item {
                    Type = Type.Up,
                    Name = ".."
                });

                CanNavigateUp = true;
            }
            else
            {
                CanNavigateUp = false;
            }

            foreach(var dir in Directory.GetDirectories(CurrentPath))
            {
                var name = Path.GetFileName(dir);

                if(name[0] == '.') continue;

                Listing.Add(new Item {
                    Type = Type.Directory,
                    Name = name
                });
            }

            foreach(var file in Directory.GetFiles(CurrentPath))
            {
                var name = Path.GetFileName(file);
                var isBackup = name.Contains('.') && name.Substring(name.LastIndexOf('.') + 1) == "authpro";

                Listing.Add(new Item {
                    Type = isBackup ? Type.Backup : Type.File,
                    Name = name
                });
            }
        }

        public bool Navigate(int position)
        {
            if(position >= Listing.Count) return false;

            var item = Listing[position];

            switch(item.Type)
            {
                case Type.Up:
                    CurrentPath = CurrentPath.Substring(0, CurrentPath.LastIndexOf('/'));
                    Update();
                    return true;

                case Type.Directory:
                    CurrentPath += $@"/{item.Name}";
                    Update();
                    return true;
            }

            return false;
        }

        public int Count()
        {
            return Listing.Count;
        }

        internal enum Type
        {
            File,
            Backup,
            Directory,
            Up
        }

        internal struct Item
        {
            public Type Type;
            public string Name;
        }
    }
}