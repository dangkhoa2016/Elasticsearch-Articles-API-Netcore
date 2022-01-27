using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using elasticsearch_netcore.Models;
using Newtonsoft.Json;

namespace elasticsearch_netcore.ViewModels
{
    public class CategoryViewModel
    {
        public CategoryViewModel()
        {
            ArticlesCategories = new HashSet<ArticlesCategoryViewModel>();
        }

        public CategoryViewModel(Category category, bool loadAssociation = false) : this()
        {
            if (category == null)
                return;

            Id = category.Id;
            Title = category.Title;
            CreatedAt = category.CreatedAt;
            UpdatedAt = category.UpdatedAt;

            if (loadAssociation)
            {
                if (category.ArticlesCategories != null)
                {
                    foreach (ArticlesCategory articlesCategory in category.ArticlesCategories)
                    {
                        ArticlesCategories.Add(new ArticlesCategoryViewModel() { Article = new ArticleViewModel(articlesCategory.Article), ArticleId = articlesCategory.ArticleId });
                    }
                }
            }
        }

        [Key]
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonPropertyName("created_at")]
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [InverseProperty(nameof(ArticlesCategory.Category))]
        public virtual ICollection<ArticlesCategoryViewModel> ArticlesCategories { get; set; }
    }
}