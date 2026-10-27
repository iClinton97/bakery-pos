using System;
using BakeryPos.Core.Catalogue;

namespace BakeryPos.Core.Orders;

/// <summary>
/// The keypad logic for typing a price, such as the Miscellaneous screen.
/// Feed it key presses ('0' to '9' and '.') and read back the amount in cents.
/// </summary>
public sealed class PriceEntry
{
    private const int MaxWholeDigits = 4;
    private const int MaxDecimalDigits = 2;

    private string _text = "";

    public bool IsEmpty => _text.Length == 0;

    /// <summary>The amount typed so far, in whole cents. Zero when nothing is typed.</summary>
    public int Cents
    {
        get
        {
            if (IsEmpty)
            {
                return 0;
            }

            var parts = _text.Split('.');
            var whole = parts[0].Length == 0 ? 0 : int.Parse(parts[0]);
            var fraction = parts.Length > 1 ? int.Parse(parts[1].PadRight(MaxDecimalDigits, '0')) : 0;
            return whole * 100 + fraction;
        }
    }

    /// <summary>The amount as dollars, for example "$3.50".</summary>
    public string DisplayText => Money.Format(Cents);

    /// <summary>Handles one key. Anything that is not a digit or a dot is ignored.</summary>
    public void Press(char key)
    {
        if (char.IsAsciiDigit(key))
        {
            PressDigit(key);
        }
        else if (key == '.')
        {
            PressDot();
        }
    }

    public void Backspace()
    {
        if (!IsEmpty)
        {
            _text = _text[..^1];
        }
    }

    public void Clear() => _text = "";

    /// <summary>Replaces what was typed with an exact amount, for quick buttons such as "$20".</summary>
    public void SetCents(int cents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cents);

        var whole = cents / 100;
        var fraction = cents % 100;
        _text = cents == 0 ? "" : fraction == 0 ? whole.ToString() : $"{whole}.{fraction:00}";
    }

    private void PressDigit(char digit)
    {
        var dot = _text.IndexOf('.');

        if (dot >= 0)
        {
            if (_text.Length - dot - 1 < MaxDecimalDigits)
            {
                _text += digit;
            }

            return;
        }

        if (_text == "0")
        {
            // Replace a lone leading zero, so "0" then "5" becomes "5".
            _text = digit == '0' ? "0" : digit.ToString();
        }
        else if (_text.Length < MaxWholeDigits)
        {
            _text += digit;
        }
    }

    private void PressDot()
    {
        if (_text.Contains('.'))
        {
            return;
        }

        _text = IsEmpty ? "0." : _text + ".";
    }
}
