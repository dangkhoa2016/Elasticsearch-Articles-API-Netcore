using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public interface ICommentRepository
    {
        Task<dynamic> GetComments(int skip = 0, int take = 10, bool loadRelation = false,
            Expression<Func<Comment, bool>> filter = null, bool showTotal = false);
        Task<CommentViewModel> GetComment(long id, bool loadRelation = false);
        Task<bool> DeleteComment(long id);
        Task<CommentViewModel> CreateComment(CommentViewModel comment);
        Task<CommentViewModel> UpdateComment(long id, CommentViewModel comment);
    }
}