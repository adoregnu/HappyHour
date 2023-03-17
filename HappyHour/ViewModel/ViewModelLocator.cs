using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MvvmDialogs;

namespace HappyHour.ViewModel
{
    class ViewModelLocator
    {
        public ViewModelLocator()
        {
            Ioc.Default.ConfigureServices(new ServiceCollection()
                .AddSingleton<MainViewModel>()
                .AddSingleton<IDialogService>(new DialogService())
                .BuildServiceProvider());
        }

        public MainViewModel MainWindow => Ioc.Default.GetService<MainViewModel>();
    }
}
