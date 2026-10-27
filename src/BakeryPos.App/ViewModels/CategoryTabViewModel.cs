using System;
using System.Windows.Input;
using BakeryPos.Core.Catalogue;

namespace BakeryPos.App.ViewModels;

public sealed class CategoryTabViewModel : ViewModelBase
{
    private bool _isSelected;

    public CategoryTabViewModel(string title, Category category, Action<Category> select)
    {
        Title = title;
        Category = category;
        SelectCommand = new RelayCommand(() => select(category));
    }

    public string Title { get; }

    public Category Category { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    public ICommand SelectCommand { get; }
}
