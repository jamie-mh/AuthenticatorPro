using System;
using SQLite;

namespace ProAuth.Data
{
    [Table("generator")]
    class Generator
    {
        [Column("id"), PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Column("implementationId")]
        public int ImplementationId { get; set; }

        [Column("secret"), MaxLength(32)]
        public string Secret { get; set; }

        [Column("ranking")]
        public int Ranking { get; set; }

        [Column("start")]
        public DateTime TimeRenew { get; set; }

        [Column("code")]
        public int Code { get; set; }
    }
}