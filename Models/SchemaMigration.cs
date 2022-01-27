using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("schema_migrations")]
    public partial class SchemaMigration
    {
        [Key]
        [Column("version", TypeName = "varchar")]
        public string Version { get; set; }
    }
}
