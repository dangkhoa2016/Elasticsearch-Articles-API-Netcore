using AutoMapper;
using elasticsearch_netcore.Models;
using elasticsearch_netcore.ViewModels;

namespace elasticsearch_netcore.Mappings
{
    /// <summary>
    /// AutoMapper profile defining mappings between Models and ViewModels.
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Article <-> ArticleViewModel
            CreateMap<Article, ArticleViewModel>()
                .ForMember(dest => dest.ArticlesCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Authorships, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            CreateMap<ArticleViewModel, Article>()
                .ForMember(dest => dest.ArticlesCategories, opt => opt.Ignore())
                .ForMember(dest => dest.Authorships, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            // Author <-> AuthorViewModel
            CreateMap<Author, AuthorViewModel>()
                .ForMember(dest => dest.Authorships, opt => opt.Ignore());

            CreateMap<AuthorViewModel, Author>();

            // Category <-> CategoryViewModel
            CreateMap<Category, CategoryViewModel>()
                .ForMember(dest => dest.ArticlesCategories, opt => opt.Ignore());

            CreateMap<CategoryViewModel, Category>();

            // Comment <-> CommentViewModel
            CreateMap<Comment, CommentViewModel>()
                .ForMember(dest => dest.Article, opt => opt.Ignore());

            CreateMap<CommentViewModel, Comment>();

            // Authorship <-> AuthorshipViewModel
            CreateMap<Authorship, AuthorshipViewModel>()
                .ForMember(dest => dest.Author, opt => opt.Ignore())
                .ForMember(dest => dest.Article, opt => opt.Ignore());

            CreateMap<AuthorshipViewModel, Authorship>();

            // ArticlesCategory <-> ArticlesCategoryViewModel
            CreateMap<ArticlesCategory, ArticlesCategoryViewModel>()
                .ForMember(dest => dest.Article, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore());

            CreateMap<ArticlesCategoryViewModel, ArticlesCategory>();
        }
    }
}
