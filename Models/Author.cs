using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("authors")]
    [Index(nameof(FirstName), nameof(LastName), Name = "index_authors_on_first_name_last_name")]
    public partial class Author
    {
        public Author()
        {
            Authorships = new HashSet<Authorship>();
        }

        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        [Column("first_name", TypeName = "varchar")]
        public string FirstName { get; set; }
        [Column("last_name", TypeName = "varchar")]
        public string LastName { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        [InverseProperty(nameof(Authorship.Author))]
        public virtual ICollection<Authorship> Authorships { get; set; }
    }
}
