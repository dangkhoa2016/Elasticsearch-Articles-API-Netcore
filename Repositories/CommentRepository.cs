using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public class CommentRepository : GenericRepository<Comment>, ICommentRepository
    {
        public CommentRepository(ElasticsearchDBContext db)
            : base(db)
        {
        }

        public async Task<dynamic> GetComments(int skip, int take = AppConstants.DefaultPageSize, bool loadRelation = false,
            Expression<Func<Comment, bool>> filter = null, bool showTotal = false)
        {
            if (_context != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > AppConstants.MaxPageSize || take <= 0)
                    take = AppConstants.DefaultPageSize;

                JArray comments = new JArray();

                var table = _context.Comments.AsQueryable().AsNoTracking();
                if (loadRelation)
                    table = table.Include(a => a.Article).AsSplitQuery();

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderByDescending(a => a.CreatedAt)
                    .Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    comments.Add(JObject.FromObject(ConvertToJObject(new CommentViewModel(r, true), loadRelation)));

                if (showTotal)
                    return new { data = comments, total = await table.CountAsync() };
                else
                    return comments;
            }

            return null;
        }

        public async Task<CommentViewModel> CreateComment(CommentViewModel comment)
        {
            if (_context != null && comment.ArticleId != null)
            {
                var record = new Comment();

                record.ArticleId = comment.ArticleId;
                record.Body = comment.Body;
                record.Pick = comment.Pick;
                record.Stars = comment.Stars;
                record.User = comment.User;
                record.UserLocation = comment.UserLocation;
                record.UpdatedAt = DateTime.Now;
                record.CreatedAt = DateTime.Now;

                var result = await _context.Comments.AddAsync(record);
                await _context.SaveChangesAsync();
                return new CommentViewModel(result.Entity);
            }

            return null;
        }

        public async Task<CommentViewModel> UpdateComment(long id, CommentViewModel comment)
        {
            if (_context != null && comment != null && id > 0 && comment.ArticleId != null)
            {
                var found = await _context.Comments.FindAsync(id);
                if (found != null)
                {
                    var entry = _context.Entry(found);
                    entry.State = EntityState.Modified;

                    found.ArticleId = comment.ArticleId;
                    found.Body = comment.Body ?? "";
                    found.Pick = comment.Pick;
                    found.Stars = comment.Stars;
                    found.User = comment.User;
                    found.UserLocation = comment.UserLocation;
                    found.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();
                    return new CommentViewModel(found);
                }
            }

            return null;
        }

        public async Task<CommentViewModel> GetComment(long id, bool loadRelation)
        {
            if (_context != null && id > 0)
            {
                var table = _context.Comments.AsQueryable().AsNoTracking();
                if (loadRelation)
                    table = table.Include(a => a.Article).AsSplitQuery();

                var record = await table.SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                {
                    return new CommentViewModel(record, true);
                }
            }

            return null;
        }

        public static JObject ConvertToJObject(CommentViewModel record, bool loadRelation = false)
        {
            JObject comment = null;
            if (loadRelation)
            {
                var article = ArticleRepository.ConvertToJObject(record.Article, false, ForPage.All);
                record.Article = null;
                comment = JObject.FromObject(record);
                comment.Remove("article_id");
                comment["article"] = article;
            }
            else
            {
                comment = JObject.FromObject(record);
                comment.Remove("article");
            }

            return comment;
        }

        public async Task<bool> DeleteComment(long id)
        {
            if (_context != null && id > 0)
            {
                _context.Comments.Remove(new Comment() { Id = id });
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}