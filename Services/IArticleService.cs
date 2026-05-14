using elasticsearch_netcore.ViewModels;
using System.Text.Json;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public interface IArticleService
    {
        Task<dynamic> GetArticlesAsync(int skip, int take, string title, bool loadRelation, bool showTotal);
        Task<string> GetArticleJsonAsync(long id);
        Task<object> GetArticleAsync(long id, bool loadRelation);
        Task<dynamic> GetCommentsForArticleAsync(long id, int skip, int take, bool showTotal);
        Task<ArticleViewModel> CreateArticleAsync(JsonElement article);
        Task<ArticleViewModel> UpdateArticleAsync(long id, JsonElement article);
        Task<bool> DeleteArticleAsync(long id);
        Task<string> ImportAsync();
    }
}
