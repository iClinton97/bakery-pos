using System;
using System.Windows.Input;

namespace BakeryPos.App.ViewModels;

/// <summary>One key on the on-screen keypad.</summary>
public sealed class KeypadKeyViewModel
{
    public KeypadKeyViewModel(string label, Action press)
    {
        Label = label;
        PressCommand = new RelayCommand(press);
    }

    public string Label { get; }

    public ICommand PressCommand { get; }
}
