using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<dynamic> GetCategoriesAsync(int skip, int take, string title, bool showTotal)
        {
            return await _categoryRepository.GetCategories(skip, take, title, showTotal);
        }

        public async Task<CategoryViewModel> GetCategoryAsync(long id)
        {
            return await _categoryRepository.GetCategory(id);
        }

        public async Task<dynamic> GetArticlesForCategoryAsync(long id, int skip, int take, string title, bool loadRelation, bool showTotal)
        {
            return await _categoryRepository.GetArticlesForCategory(id, skip, take, title, loadRelation, showTotal);
        }

        public async Task<CategoryViewModel> CreateCategoryAsync(CategoryViewModel category)
        {
            return await _categoryRepository.CreateCategory(category);
        }

        public async Task<CategoryViewModel> UpdateCategoryAsync(long id, CategoryViewModel category)
        {
            return await _categoryRepository.UpdateCategory(id, category);
        }

        public async Task<bool> DeleteCategoryAsync(long id)
        {
            return await _categoryRepository.DeleteCategory(id);
        }
    }
}
