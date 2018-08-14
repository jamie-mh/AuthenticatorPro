using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using SQLite;

namespace ProAuth.Data
{
    [Table("generator")]
    class Generator
    {
        [PrimaryKey, AutoIncrement, Column("_id")]
        public int id { get; set; }

        [MaxLength(32)]
        public string secret { get; set; }
    }
}