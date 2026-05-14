using elasticsearch_netcore.ViewModels;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public interface ICommentService
    {
        Task<dynamic> GetCommentsAsync(int skip, int take, bool loadRelation, bool showTotal);
        Task<string> GetCommentAsync(long id, bool loadRelation);
        Task<CommentViewModel> CreateCommentAsync(CommentViewModel comment);
        Task<CommentViewModel> UpdateCommentAsync(long id, CommentViewModel comment);
        Task<bool> DeleteCommentAsync(long id);
    }
}
