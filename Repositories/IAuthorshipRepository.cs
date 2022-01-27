using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public interface IAuthorshipRepository
    {
        Task<dynamic> GetAuthorships(int skip = 0, int take = 10, bool loadRelation = false
            , Expression<Func<Authorship, bool>> filter = null, bool showTotal = false);
        Task<AuthorshipViewModel> GetAuthorship(long id, bool loadRelation = false);
        Task<bool> DeleteAuthorship(long id);
        Task<AuthorshipViewModel> CreateAuthorship(AuthorshipViewModel authorship);
        Task<AuthorshipViewModel> UpdateAuthorship(long id, AuthorshipViewModel authorship);
    }
}