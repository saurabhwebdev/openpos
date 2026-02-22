using System.ComponentModel;

namespace MyWinFormsApp.Models;

public class CartItem : INotifyPropertyChanged
{
    private decimal _quantity;

    public Product Product { get; set; } = null!;
    public decimal TaxRate { get; set; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(LineSubtotal));
                OnPropertyChanged(nameof(TaxAmount));
                OnPropertyChanged(nameof(LineTotal));
                OnPropertyChanged(nameof(FormattedQuantity));
                OnPropertyChanged(nameof(FormattedLineTotal));
            }
        }
    }

    public decimal LineSubtotal => Product.SellingPrice * Quantity;

    // Tax inclusive: tax = price - (price / (1 + rate/100))
    public decimal TaxAmount
    {
        get
        {
            if (TaxRate == 0) return 0;
            return LineSubtotal - (LineSubtotal / (1 + TaxRate / 100));
        }
    }

    public decimal LineTotal => LineSubtotal;

    // Display
    public string FormattedQuantity => $"{Quantity:0.##}";
    public string FormattedUnitPrice => $"\u20b9{Product.SellingPrice:N2}";
    public string FormattedLineTotal => $"\u20b9{LineTotal:N2}";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
