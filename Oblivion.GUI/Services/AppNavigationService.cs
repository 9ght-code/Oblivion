using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Oblivion.GUI.Domain.Abstractions;

namespace Oblivion.GUI.Services
{
    public partial class AppNavigationService(IServiceProvider serviceProvider) : ObservableObject
    {

        [ObservableProperty]
        private ViewModelBase currentHomePage;

        [ObservableProperty]
        private ViewModelBase currentMainWindowPage;

        public void NavigateTo<T>() where T : ViewModelBase
            => CurrentMainWindowPage = serviceProvider.GetRequiredService<T>();

        public void ChangePageTo<T>() where T : ViewModelBase
            => CurrentHomePage = serviceProvider.GetRequiredService<T>();
    }
}
