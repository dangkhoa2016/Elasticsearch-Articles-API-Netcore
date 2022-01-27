using elasticsearch_netcore.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace elasticsearch_netcore.ViewModels
{
    public class ArticleViewModel
    {
        public ArticleViewModel()
        {
            ArticlesCategories = new HashSet<ArticlesCategoryViewModel>();
            Authorships = new HashSet<AuthorshipViewModel>();
            Comments = new HashSet<CommentViewModel>();
        }

        public ArticleViewModel(Article article, bool loadAssociation = false) : this()
        {
            if (article == null)
                return;

            Id = article.Id;
            Title = article.Title;
            Content = article.Content;
            PublishedOn = article.PublishedOn;
            CreatedAt = article.CreatedAt;
            UpdatedAt = article.UpdatedAt;
            Abstract = article.Abstract;
            Url = article.Url;
            Shares = article.Shares;

            if (loadAssociation)
            {
                if (article.ArticlesCategories != null)
                {
                    foreach (ArticlesCategory articlesCategory in article.ArticlesCategories)
                    {
                        ArticlesCategories.Add(new ArticlesCategoryViewModel()
                        {
                            Category = articlesCategory.Category != null ? new CategoryViewModel(articlesCategory.Category) : null,
                            CategoryId = articlesCategory.CategoryId
                        });
                    }
                }

                if (article.Authorships != null)
                {
                    foreach (Authorship authorships in article.Authorships)
                    {
                        Authorships.Add(new AuthorshipViewModel()
                        {
                            Author = authorships.Author != null ? new AuthorViewModel(authorships.Author) : null,
                            AuthorId = authorships.AuthorId
                        });
                    }
                }

                if (article.Comments != null)
                {
                    foreach (Comment comment in article.Comments)
                    {
                        Comments.Add(new CommentViewModel(comment));
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

        [JsonPropertyName("content")]
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonPropertyName("published_on")]
        [JsonProperty("published_on")]
        public DateTime? PublishedOn { get; set; }

        [JsonPropertyName("created_at")]
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("abstract")]
        [JsonProperty("abstract")]
        public string Abstract { get; set; }

        [JsonPropertyName("url")]
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonPropertyName("shares")]
        [JsonProperty("shares")]
        public long? Shares { get; set; }

        [InverseProperty(nameof(ArticlesCategory.Article))]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ICollection<ArticlesCategoryViewModel> ArticlesCategories { get; set; }

        [InverseProperty(nameof(Authorship.Article))]
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual ICollection<AuthorshipViewModel> Authorships { get; set; }

        [InverseProperty(nameof(Comment.Article))]
        [JsonPropertyName("comments")]
        [JsonProperty("comments")]
        public virtual ICollection<CommentViewModel> Comments { get; set; }

        public JObject AsIndexedJson()
        {
            var json = JObject.FromObject(this);
            json["comments"] = JArray.FromObject(Comments.Select(c => c.AsIndexedJson()));
            json["categories"] = JArray.FromObject(ArticlesCategories.Select(ac => ac.Category.Title));
            json["authors"] = JArray.FromObject(Authorships.Select(a => new { full_name = a.Author.FullName() }));

            return json;
        }
    }
}