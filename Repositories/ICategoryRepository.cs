using elasticsearch_netcore.Constants;
using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Repositories
{
    public interface ICategoryRepository
    {
        Task<dynamic> GetCategories(int skip = 0, int take = AppConstants.DefaultPageSize
            , Expression<Func<Category, bool>> filter = null, bool showTotal = false);
        Task<dynamic> GetCategories(int skip = 0, int take = AppConstants.DefaultPageSize, string title = "", bool showTotal = false);
        Task<CategoryViewModel> GetCategory(long id);
        Task<bool> DeleteCategory(long id);
        Task<CategoryViewModel> CreateCategory(CategoryViewModel category);
        Task<CategoryViewModel> UpdateCategory(long id, CategoryViewModel category);
        Task<dynamic> GetArticlesForCategory(long id, int skip = 0, int take = AppConstants.DefaultPageSize, string title = "",
             bool loadRelation = false, bool showTotal = false);
    }
}