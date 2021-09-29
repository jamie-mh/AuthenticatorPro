using AuthenticatorPro.Shared.Persistence;
using System.Threading.Tasks;
using Xunit;

namespace AuthenticatorPro.Test
{
    public abstract class DataTest : IAsyncLifetime
    {
        protected readonly IAuthenticatorRepository AuthenticatorRepository;
        protected readonly IAuthenticatorCategoryRepository AuthenticatorCategoryRepository;
        protected readonly ICategoryRepository CategoryRepository;
        protected readonly ICustomIconRepository CustomIconRepository;

        public DataTest(IAuthenticatorRepository authenticatorRepository,
            ICategoryRepository categoryRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            ICustomIconRepository customIconRepository)
        {
            AuthenticatorRepository = authenticatorRepository;
            AuthenticatorCategoryRepository = authenticatorCategoryRepository;
            CategoryRepository = categoryRepository;
            CustomIconRepository = customIconRepository;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            var authenticators = await AuthenticatorRepository.GetAllAsync();

            for (var i = 0; i < authenticators.Count; i++)
            {
                var authenticator = authenticators[i];
                await AuthenticatorRepository.DeleteAsync(authenticator);
            }

            var categories = await CategoryRepository.GetAllAsync();

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                await CategoryRepository.DeleteAsync(category);
            }

            var authenticatorCategories = await AuthenticatorCategoryRepository.GetAllAsync();

            for (var i = 0; i < authenticatorCategories.Count; i++)
            {
                var authenticatorCategory = authenticatorCategories[i];
                await AuthenticatorCategoryRepository.DeleteAsync(authenticatorCategory);
            }

            var customIcons = await CustomIconRepository.GetAllAsync();

            for (var i = 0; i < customIcons.Count; i++)
            {
                var customIcon = customIcons[i];
                await CustomIconRepository.DeleteAsync(customIcon);
            }
        }
    }
}