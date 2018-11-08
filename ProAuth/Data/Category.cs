using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Android.Service.Notification;
using Android.Widget;
using Newtonsoft.Json;
using OtpSharp;
using ProAuth.Utilities;
using SQLite;

namespace ProAuth.Data
{
    [Table("category")]
    internal class Category
    {
        [Column("id")]
        public String Id { get; set; }

        [Column("name")]
        public String Name { get; set; }

        [Column("icon")]
        public string Icon { get; set; }

        public Category()
        {

        }

        public Category(string name, string icon = null)
        {
            Id = name.GetSlug();
            Name = name;
            Icon = icon;
        }
    }
}