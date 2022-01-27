using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("articles_categories")]
    [Index(nameof(ArticleId), Name = "index_articles_categories_on_article_id")]
    [Index(nameof(CategoryId), Name = "index_articles_categories_on_category_id")]
    public partial class ArticlesCategory
    {
        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        [Column("article_id", TypeName = "integer")]
        public long? ArticleId { get; set; }
        [Column("category_id", TypeName = "integer")]
        public long? CategoryId { get; set; }

        [ForeignKey(nameof(ArticleId))]
        [InverseProperty("ArticlesCategories")]
        public virtual Article Article { get; set; }
        [ForeignKey(nameof(CategoryId))]
        [InverseProperty("ArticlesCategories")]
        public virtual Category Category { get; set; }
    }
}
