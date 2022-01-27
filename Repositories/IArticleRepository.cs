using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public interface IArticleRepository
    {
        Task<dynamic> GetArticles(int skip = 0, int take = 10, bool loadRelation = false
            , Expression<Func<Article, bool>> filter = null, bool showTotal = false);
        Task<dynamic> GetArticles(int skip = 0, int take = 10
            , string title = "", bool loadRelation = false, bool showTotal = false);
        Task<ArticleViewModel> GetArticle(long id, bool loadRelation = false);
        Task<bool> DeleteArticle(long id);
        Task<ArticleViewModel> CreateArticle(ArticleViewModel article);
        Task<ArticleViewModel> UpdateArticle(long id, ArticleViewModel article);
        Task<dynamic> GetCommentsForArticle(long id, int skip = 0, int take = 10, bool showTotal = false);
        Task BulkIndex();
    }
}