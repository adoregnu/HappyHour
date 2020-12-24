using GalaSoft.MvvmLight.Ioc;
using MvvmDialogs;

namespace HappyHour.ViewModel
{
    class ViewModelLocator
    {
        public ViewModelLocator()
        {
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<IDialogService>(() => new DialogService());
        }

        public MainViewModel MainWindow => SimpleIoc.Default.GetInstance<MainViewModel>();
    }
}
