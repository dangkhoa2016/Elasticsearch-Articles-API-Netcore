using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public interface IAuthorRepository
    {
        Task<dynamic> GetAuthors(int skip = 0, int take = 10
            , Expression<Func<Author, bool>> filter = null, bool showTotal = false);
        Task<dynamic> GetAuthors(int skip = 0, int take = 10, string name = "", bool showTotal = false);
        Task<AuthorViewModel> GetAuthor(long id);
        Task<bool> DeleteAuthor(long id);
        Task<AuthorViewModel> CreateAuthor(AuthorViewModel author);
        Task<AuthorViewModel> UpdateAuthor(long id, AuthorViewModel author);
        Task<dynamic> GetArticlesForAuthor(long id, int skip, int take, string title = "",
            bool loadRelation = false, bool showTotal = false);
    }
}