using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Albireo.Base32;
using Javax.Xml.Xpath;
using OtpSharp;
using ProAuth.Data;
using SQLite;

namespace ProAuth.Utilities
{
    internal class FilesystemSource
    {
        public bool CanNavigateUp;
        public string CurrentPath { get; private set; }
        public List<Item> Listing { get; private set; }

        private readonly string _root;

        public FilesystemSource(string root)
        {
            _root = root;
            CurrentPath = root;
            CanNavigateUp = false;

            Listing = new List<Item>();
            Update();
        }

        public void Update()
        {
            Listing.Clear();

            if(CurrentPath != _root)
            {
                Listing.Add(new Item
                {
                    Type = Type.Up,
                    Name = ".."
                });

                CanNavigateUp = true;
            }
            else
            {
                CanNavigateUp = false;
            }

            foreach(string dir in Directory.GetDirectories(CurrentPath))
            {
                string name = Path.GetFileName(dir);

                if(name[0] == '.')
                {
                    continue;
                }

                Listing.Add(new Item
                {
                    Type = Type.Directory,
                    Name = name
                });
            }

            foreach(string file in Directory.GetFiles(CurrentPath))
            {
                string name = Path.GetFileName(file);
                bool isBackup = name.Substring(name.LastIndexOf('.') + 1) == "proauth";

                Listing.Add(new Item 
                {
                    Type = isBackup ? Type.Backup : Type.File, 
                    Name = name 
                });
            }
        }

        public void Navigate(int position)
        {
            if(position >= Listing.Count)
            {
                return;
            }

            Item item = Listing[position];

            switch (item.Type)
            {
                case Type.Up:
                    CurrentPath = CurrentPath.Substring(0, CurrentPath.LastIndexOf('/'));
                    Update();
                    break;

                case Type.Directory:
                    CurrentPath += $@"/{item.Name}";
                    Update();
                    break;
            }
        }

        public int Count()
        {
            return Listing.Count;
        }

        internal enum Type
        {
            File, Backup, Directory, Up
        }

        internal struct Item
        {
            public Type Type;
            public string Name;
        }
    }
}