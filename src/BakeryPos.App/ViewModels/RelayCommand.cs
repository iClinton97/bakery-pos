using System;
using System.Windows.Input;

namespace BakeryPos.App.ViewModels;

/// <summary>A button command that runs a method. These commands are always enabled.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    // Never raised, because these commands are always enabled.
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
