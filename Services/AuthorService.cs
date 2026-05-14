using elasticsearch_netcore.Repositories;
using elasticsearch_netcore.ViewModels;
using System.Threading.Tasks;

namespace elasticsearch_netcore.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _authorRepository;

        public AuthorService(IAuthorRepository authorRepository)
        {
            _authorRepository = authorRepository;
        }

        public async Task<dynamic> GetAuthorsAsync(int skip, int take, string name, bool showTotal)
        {
            return await _authorRepository.GetAuthors(skip, take, name, showTotal);
        }

        public async Task<AuthorViewModel> GetAuthorAsync(long id)
        {
            return await _authorRepository.GetAuthor(id);
        }

        public async Task<dynamic> GetArticlesForAuthorAsync(long id, int skip, int take, string title, bool loadRelation, bool showTotal)
        {
            return await _authorRepository.GetArticlesForAuthor(id, skip, take, title, loadRelation, showTotal);
        }

        public async Task<AuthorViewModel> CreateAuthorAsync(AuthorViewModel author)
        {
            if (!IsValid(author))
                return null;

            return await _authorRepository.CreateAuthor(author);
        }

        public async Task<AuthorViewModel> UpdateAuthorAsync(long id, AuthorViewModel author)
        {
            if (!IsValid(author))
                return null;

            return await _authorRepository.UpdateAuthor(id, author);
        }

        public async Task<bool> DeleteAuthorAsync(long id)
        {
            return await _authorRepository.DeleteAuthor(id);
        }

        bool IsValid(AuthorViewModel author)
        {
            return !string.IsNullOrWhiteSpace(author.FirstName) || !string.IsNullOrWhiteSpace(author.LastName);
        }
    }
}
