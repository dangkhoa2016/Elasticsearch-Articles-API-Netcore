using elasticsearch_netcore.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace elasticsearch_netcore.ViewModels
{
    public class CommentViewModel
    {
        public CommentViewModel()
        {

        }

        public CommentViewModel(Comment comment, bool loadAssociation = false)
        {
            if (comment == null)
                return;

            Id = comment.Id;
            Body = comment.Body;
            User = comment.User;
            UserLocation = comment.UserLocation;
            Stars = comment.Stars;
            Pick = comment.Pick;
            ArticleId = comment.ArticleId;
            CreatedAt = comment.CreatedAt;
            UpdatedAt = comment.UpdatedAt;

            if (loadAssociation)
            {
                if (comment.Article != null)
                {
                    Article = new ArticleViewModel(comment.Article);
                }
            }
        }

        [Key]
        [JsonPropertyName("id")]
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonPropertyName("body")]
        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonPropertyName("user")]
        [JsonProperty("user")]
        public string User { get; set; }

        [JsonPropertyName("user_location")]
        [JsonProperty("user_location")]
        public string UserLocation { get; set; }

        [JsonPropertyName("stars")]
        [JsonProperty("stars")]
        public long? Stars { get; set; }

        [JsonPropertyName("pick")]
        [JsonProperty("pick")]
        public bool? Pick { get; set; }

        [JsonPropertyName("article_id")]
        [JsonProperty("article_id")]
        public long? ArticleId { get; set; }

        [JsonPropertyName("created_at")]
        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        [JsonProperty("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("article")]
        [JsonProperty("article")]
        [ForeignKey(nameof(ArticleId))]
        [InverseProperty("Comments")]
        public virtual ArticleViewModel Article { get; set; }

        public JObject AsIndexedJson()
        {
            return JObject.FromObject(new
            {
                body = Body,
                star = Stars,
                pick = Pick,
                user = User,
                user_location = UserLocation
            });
        }
    }
}