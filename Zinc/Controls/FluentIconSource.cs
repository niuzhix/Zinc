using FluentAvalonia.UI.Controls;

namespace Zinc.Controls;

public class FluentIconSource : FASymbolIconSource
{
    public FluentIconSource(FASymbol symbol)
    {
        Symbol = symbol;
    }

    public FluentIconSource ProvideValue() => this;
}