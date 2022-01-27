using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using elasticsearch_netcore.Models;
using Newtonsoft.Json;

namespace elasticsearch_netcore.ViewModels
{
    public class AuthorViewModel
    {
        public AuthorViewModel()
        {
            Authorships = new HashSet<AuthorshipViewModel>();
        }

        public AuthorViewModel(Author author, bool loadAssociation = false) : this()
        {
            if (author == null)
                return;

            Id = author.Id;
            FirstName = author.FirstName;
            LastName = author.LastName;
            CreatedAt = author.CreatedAt;
            UpdatedAt = author.UpdatedAt;

            if (loadAssociation)
            {
                if (author.Authorships != null)
                {
                    foreach (Authorship authorship in author.Authorships)
                    {
                        Authorships.Add(new AuthorshipViewModel() { Article = new ArticleViewModel(authorship.Article), ArticleId = authorship.ArticleId });
                    }
                }
            }
        }

        [Key]
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonPropertyName("first_name")]
        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("created_at")]
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [InverseProperty(nameof(Authorship.Author))]
        public virtual ICollection<AuthorshipViewModel> Authorships { get; set; }

        public string FullName()
        {
            return FirstName + " " + LastName;
        }
    }
}