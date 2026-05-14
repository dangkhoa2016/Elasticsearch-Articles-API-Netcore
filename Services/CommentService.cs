using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;

        public CommentService(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<dynamic> GetCommentsAsync(int skip, int take, bool loadRelation, bool showTotal)
        {
            return await _commentRepository.GetComments(skip, take, loadRelation, null, showTotal);
        }

        public async Task<string> GetCommentAsync(long id, bool loadRelation)
        {
            var record = await _commentRepository.GetComment(id, loadRelation);
            if (record == null)
                return null;

            return CommentRepository.ConvertToJObject(record, loadRelation).ToString(Formatting.None);
        }

        public async Task<CommentViewModel> CreateCommentAsync(CommentViewModel comment)
        {
            return await _commentRepository.CreateComment(comment);
        }

        public async Task<CommentViewModel> UpdateCommentAsync(long id, CommentViewModel comment)
        {
            return await _commentRepository.UpdateComment(id, comment);
        }

        public async Task<bool> DeleteCommentAsync(long id)
        {
            return await _commentRepository.DeleteComment(id);
        }
    }
}
