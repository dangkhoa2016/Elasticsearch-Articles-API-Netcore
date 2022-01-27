using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("comments")]
    [Index(nameof(ArticleId), Name = "index_comments_on_article_id")]
    public partial class Comment
    {
        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        [Column("body", TypeName = "text")]
        public string? Body { get; set; }
        [Column("user", TypeName = "varchar")]
        public string User { get; set; }
        [Column("user_location", TypeName = "varchar")]
        public string UserLocation { get; set; }
        [Column("stars", TypeName = "integer")]
        public long? Stars { get; set; }
        [Column("pick", TypeName = "boolean")]
        public bool? Pick { get; set; }
        [Column("article_id", TypeName = "integer")]
        public long? ArticleId { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ArticleId))]
        [InverseProperty("Comments")]
        public virtual Article Article { get; set; }
    }
}
