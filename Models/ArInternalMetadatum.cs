using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("ar_internal_metadata")]
    public partial class ArInternalMetadatum
    {
        [Key]
        [Column("key", TypeName = "varchar")]
        public string Key { get; set; }
        [Column("value", TypeName = "varchar")]
        public string Value { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public byte[] CreatedAt { get; set; }
        [Column("updated_at", TypeName = "datetime")]
        public byte[] UpdatedAt { get; set; }
    }
}
