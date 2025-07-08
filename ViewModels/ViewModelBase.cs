using CommunityToolkit.Mvvm.ComponentModel;

namespace HealthyMeal.ViewModels
{
    /// <summary>
    /// A base class for all ViewModels in the application.
    /// It inherits from ObservableObject to provide INotifyPropertyChanged implementation.
    /// </summary>
    public abstract partial class ViewModelBase : ObservableObject
    {
    }
}