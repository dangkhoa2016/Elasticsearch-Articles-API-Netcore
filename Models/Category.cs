using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace elasticsearch_netcore.Models
{
    [Table("categories")]
    public partial class Category
    {
        public Category()
        {
            ArticlesCategories = new HashSet<ArticlesCategory>();
        }

        [Key]
        [Column("id", TypeName = "integer")]
        public long Id { get; set; }
        [Required]
        [Column("title", TypeName = "varchar")]
        public string Title { get; set; }
        [Column("created_at", TypeName = "datetime")]
        public DateTime?  CreatedAt { get; set; } = DateTime.Now;
        [Column("updated_at", TypeName = "datetime")]
        public DateTime? UpdatedAt { get; set; } = DateTime.Now;

        [InverseProperty(nameof(ArticlesCategory.Category))]
        public virtual ICollection<ArticlesCategory> ArticlesCategories { get; set; }
    }
}
