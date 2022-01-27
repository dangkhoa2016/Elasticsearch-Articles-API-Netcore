using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("authorships")]
    [Index(nameof(ArticleId), Name = "index_authorships_on_article_id")]
    [Index(nameof(AuthorId), Name = "index_authorships_on_author_id")]
    public partial class Authorship
    {
        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        [Column("article_id", TypeName = "integer")]
        public long? ArticleId { get; set; }
        [Column("author_id", TypeName = "integer")]
        public long? AuthorId { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ArticleId))]
        [InverseProperty("Authorships")]
        public virtual Article Article { get; set; }
        [ForeignKey(nameof(AuthorId))]
        [InverseProperty("Authorships")]
        public virtual Author Author { get; set; }
    }
}
