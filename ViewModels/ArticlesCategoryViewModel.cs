using elasticsearch_netcore.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace elasticsearch_netcore.ViewModels
{
    public partial class ArticlesCategoryViewModel
    {
        public ArticlesCategoryViewModel()
        {

        }

        public ArticlesCategoryViewModel(ArticlesCategory articlesCategory, bool loadAssociation = false)
        {
            if (articlesCategory == null)
                return;

            ArticleId = articlesCategory.ArticleId;
            CategoryId = articlesCategory.CategoryId;

            if (loadAssociation)
            {
                if (articlesCategory.Article != null)
                    Article = new ArticleViewModel(articlesCategory.Article);
                if (articlesCategory.Category != null)
                    Category = new CategoryViewModel(articlesCategory.Category);
            }
        }


        [Key]
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonPropertyName("article_id")]
        [JsonProperty("article_id")]
        public long? ArticleId { get; set; }

        [JsonPropertyName("category_id")]
        [JsonProperty("category_id")]
        public long? CategoryId { get; set; }

        [ForeignKey(nameof(ArticleId))]
        [InverseProperty("ArticlesCategories")]
        [JsonPropertyName("article")]
        [JsonProperty("article")]
        public virtual ArticleViewModel Article { get; set; }

        [ForeignKey(nameof(CategoryId))]
        [InverseProperty("ArticlesCategories")]
        [JsonPropertyName("comments")]
        [JsonProperty("category")]
        public virtual CategoryViewModel Category { get; set; }
    }
}
