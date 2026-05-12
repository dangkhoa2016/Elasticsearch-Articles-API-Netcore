using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable enable
namespace elasticsearch_netcore.Models
{
    #nullable disable
    [Table("comments")]
    [Index(nameof(ArticleId), Name = "index_comments_on_article_id")]
    public partial class Comment
    {
        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        #nullable enable
        [Column("body", TypeName = "text")]
        public string? Body { get; set; }
        #nullable disable
        [Column("user", TypeName = "varchar")]
        public string User { get; set; } = null!;
        [Column("user_location", TypeName = "varchar")]
        public string UserLocation { get; set; } = null!;
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
        public virtual Article Article { get; set; } = null!;
    }
}
