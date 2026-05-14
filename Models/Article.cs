using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

#nullable enable
namespace elasticsearch_netcore.Models
{
    #nullable disable
    [Table("articles")]
    [Index(nameof(Title), Name = "index_articles_on_title")]
    [Index(nameof(CreatedAt), Name = "index_articles_on_created_at")]
    public partial class Article
    {
        public Article()
        {
            ArticlesCategories = new HashSet<ArticlesCategory>();
            Authorships = new HashSet<Authorship>();
            Comments = new HashSet<Comment>();
        }

        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        [Required]
        [Column("title", TypeName = "varchar")]
        public string Title { get; set; } = null!;
        [Required]
        [Column("content", TypeName = "text")]
        public string Content { get; set; } = null!;
        [Column("published_on", TypeName = "date")]
        public DateTime? PublishedOn { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
#nullable enable
        [Column("abstract", TypeName = "text")]
        public string? Abstract { get; set; }
        [Column("url", TypeName = "varchar")]
        public string? Url { get; set; }
#nullable disable
        [Column("shares", TypeName = "integer")]
        public long? Shares { get; set; }

        [InverseProperty(nameof(ArticlesCategory.Article))]
        public virtual ICollection<ArticlesCategory> ArticlesCategories { get; set; }
        [InverseProperty(nameof(Authorship.Article))]
        public virtual ICollection<Authorship> Authorships { get; set; }
        [InverseProperty(nameof(Comment.Article))]
        public virtual ICollection<Comment> Comments { get; set; }
    }
}
