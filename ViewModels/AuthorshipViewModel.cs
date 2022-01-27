using elasticsearch_netcore.Models;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace elasticsearch_netcore.ViewModels
{
    public class AuthorshipViewModel
    {
        public AuthorshipViewModel()
        {

        }

        public AuthorshipViewModel(Authorship authorship, bool loadAssociation = false)
        {
            if (authorship == null)
                return;

            Id = authorship.Id;
            ArticleId = authorship.ArticleId;
            AuthorId = authorship.AuthorId;
            CreatedAt = authorship.CreatedAt;
            UpdatedAt = authorship.UpdatedAt;

            if (loadAssociation)
            {
                if (authorship.Author != null)
                    Author = new AuthorViewModel(authorship.Author);
                if (authorship.Article != null)
                    Article = new ArticleViewModel(authorship.Article);
            }
        }

        [Key]
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonPropertyName("article_id")]
        [JsonProperty("article_id")]
        public long? ArticleId { get; set; }

        [JsonPropertyName("author_id")]
        [JsonProperty("author_id")]
        public long? AuthorId { get; set; }

        [JsonPropertyName("created_at")]
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(ArticleId))]
        [InverseProperty("Authorships")]
        [JsonPropertyName("article")]
        [JsonProperty("article")]
        public virtual ArticleViewModel Article { get; set; }

        [ForeignKey(nameof(AuthorId))]
        [InverseProperty("Authorships")]
        [JsonPropertyName("author")]
        [JsonProperty("author")]
        public virtual AuthorViewModel Author { get; set; }
    }
}