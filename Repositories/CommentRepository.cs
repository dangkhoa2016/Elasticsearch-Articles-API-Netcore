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
    public class CommentRepository : ICommentRepository
    {
        private ElasticsearchDBContext db;

        public CommentRepository(ElasticsearchDBContext db)
        {
            this.db = db;
        }

        public async Task<dynamic> GetComments(int skip, int take = 10, bool loadRelation = false,
            Expression<Func<Comment, bool>> filter = null, bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray comments = new JArray();

                var table = db.Comments.AsQueryable().AsNoTracking();
                if (loadRelation)
                    table = table.Include(a => a.Article);

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
            if (db != null && comment.ArticleId != null)
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

                var result = await db.Comments.AddAsync(record);
                await db.SaveChangesAsync();
                return new CommentViewModel(result.Entity);
            }

            return null;
        }

        public async Task<CommentViewModel> UpdateComment(long id, CommentViewModel comment)
        {
            if (db != null && comment != null && id > 0 && comment.ArticleId != null)
            {
                var found = await db.Comments.FindAsync(id);
                if (found != null)
                {
                    var entry = db.Entry(found);
                    entry.State = EntityState.Modified;

                    found.ArticleId = comment.ArticleId;
                    found.Body = comment.Body ?? "";
                    found.Pick = comment.Pick;
                    found.Stars = comment.Stars;
                    found.User = comment.User;
                    found.UserLocation = comment.UserLocation;
                    found.UpdatedAt = DateTime.Now;

                    await db.SaveChangesAsync();
                    return new CommentViewModel(found);
                }
            }

            return null;
        }

        public async Task<CommentViewModel> GetComment(long id, bool loadRelation)
        {
            if (db != null && id > 0)
            {
                var table = db.Comments.AsQueryable().AsNoTracking();
                if (loadRelation)
                    table = table.Include(a => a.Article);

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
            if (db != null && id > 0)
            {
                db.Comments.Remove(new Comment() { Id = id });
                await db.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}