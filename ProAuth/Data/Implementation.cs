using Newtonsoft.Json;
using SQLite;

namespace ProAuth.Data
{
    [Table("implementation")]
    class Implementation
    {
        [JsonProperty(PropertyName = "id")]
        [Column("id"), PrimaryKey]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        [Column("name")]
        public string Name { get; set; }
    }
}