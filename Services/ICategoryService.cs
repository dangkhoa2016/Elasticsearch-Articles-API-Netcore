using elasticsearch_netcore.ViewModels;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public interface ICategoryService
    {
        Task<dynamic> GetCategoriesAsync(int skip, int take, string title, bool showTotal);
        Task<CategoryViewModel> GetCategoryAsync(long id);
        Task<dynamic> GetArticlesForCategoryAsync(long id, int skip, int take, string title, bool loadRelation, bool showTotal);
        Task<CategoryViewModel> CreateCategoryAsync(CategoryViewModel category);
        Task<CategoryViewModel> UpdateCategoryAsync(long id, CategoryViewModel category);
        Task<bool> DeleteCategoryAsync(long id);
    }
}
