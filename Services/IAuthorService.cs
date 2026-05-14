using elasticsearch_netcore.ViewModels;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public interface IAuthorService
    {
        Task<dynamic> GetAuthorsAsync(int skip, int take, string name, bool showTotal);
        Task<AuthorViewModel> GetAuthorAsync(long id);
        Task<dynamic> GetArticlesForAuthorAsync(long id, int skip, int take, string title, bool loadRelation, bool showTotal);
        Task<AuthorViewModel> CreateAuthorAsync(AuthorViewModel author);
        Task<AuthorViewModel> UpdateAuthorAsync(long id, AuthorViewModel author);
        Task<bool> DeleteAuthorAsync(long id);
    }
}
