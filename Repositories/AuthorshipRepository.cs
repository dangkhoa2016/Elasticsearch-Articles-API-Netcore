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
    public class AuthorshipRepository : IAuthorshipRepository
    {
        private ElasticsearchDBContext db;

        public AuthorshipRepository(ElasticsearchDBContext db)
        {
            this.db = db;
        }

        public async Task<dynamic> GetAuthorships(int skip, int take = 10, bool loadRelation = false,
            Expression<Func<Authorship, bool>> filter = null, bool showTotal = false)
        {
            if (db != null)
            {
                if (skip < 0)
                    skip = 0;
                if (take > 50 || take <= 0)
                    take = 10;

                JArray authorships = new JArray();

                var table = db.Authorships.AsQueryable().AsNoTracking();

                if (loadRelation)
                {
                    table = table.Include(a => a.Article)
                                .Include(a => a.Author);
                }

                if (filter != null)
                    table = table.Where(filter);

                var records = await table.OrderBy(a => a.ArticleId).ThenBy(a => a.AuthorId).Skip(skip).Take(take).ToListAsync();

                foreach (var r in records)
                    authorships.Add(ConvertToJObject(new AuthorshipViewModel(r, true), loadRelation));

                if (showTotal)
                    return new { data = authorships, total = await table.CountAsync() };
                else
                    return authorships;
            }

            return null;
        }

        public async Task<AuthorshipViewModel> CreateAuthorship(AuthorshipViewModel authorship)
        {
            if (db != null)
            {
                var record = new Authorship();

                record.ArticleId = authorship.ArticleId;
                record.AuthorId = authorship.AuthorId;
                record.UpdatedAt = DateTime.Now;
                record.CreatedAt = DateTime.Now;

                var result = await db.Authorships.AddAsync(record);
                await db.SaveChangesAsync();
                return new AuthorshipViewModel(result.Entity);
            }

            return null;
        }

        public async Task<AuthorshipViewModel> UpdateAuthorship(long id, AuthorshipViewModel authorship)
        {
            if (db != null && authorship != null && id > 0)
            {
                var found = await db.Authorships.FindAsync(id);
                if (found != null)
                {
                    var entry = db.Entry(found);
                    entry.State = EntityState.Modified;

                    found.ArticleId = authorship.ArticleId;
                    found.AuthorId = authorship.AuthorId;
                    found.UpdatedAt = DateTime.Now;

                    await db.SaveChangesAsync();
                    return new AuthorshipViewModel(found);
                }
            }

            return null;
        }

        public async Task<AuthorshipViewModel> GetAuthorship(long id, bool loadRelation)
        {
            if (db != null && id > 0)
            {
                var table = db.Authorships.AsQueryable().AsNoTracking();
                if (loadRelation)
                {
                    table = table.Include(a => a.Article)
                                .Include(a => a.Author);
                }

                var record = await table.SingleOrDefaultAsync(a => a.Id == id);
                if (record != null)
                {
                    return new AuthorshipViewModel(record, true);
                }
            }

            return null;
        }

        public static JObject ConvertToJObject(AuthorshipViewModel record, bool loadRelation = false)
        {
            JObject authorship = JObject.FromObject(record);
            if (loadRelation)
            {
                authorship.Remove("author_id");
                (authorship.SelectToken("article") as JObject).Remove("comments");
                authorship.Remove("article_id");
            }
            else
            {
                authorship.Remove("author");
                authorship.Remove("article");
            }

            return authorship;
        }

        public async Task<bool> DeleteAuthorship(long id)
        {
            if (db != null && id > 0)
            {
                db.Authorships.Remove(new Authorship() { Id = id });
                await db.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}