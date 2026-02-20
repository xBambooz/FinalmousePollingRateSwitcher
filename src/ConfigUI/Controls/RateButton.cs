using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Finalmouse.ConfigUI.Controls;

public class RateButton : Border
{
    public static readonly DependencyProperty RateHzProperty =
        DependencyProperty.Register("RateHz", typeof(int), typeof(RateButton), new PropertyMetadata(1000, OnRateChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register("IsSelected", typeof(bool), typeof(RateButton), new PropertyMetadata(false, OnSelectedChanged));

    public static readonly RoutedEvent RateSelectedEvent =
        EventManager.RegisterRoutedEvent("RateSelected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RateButton));

    private readonly TextBlock _label;

    private static readonly SolidColorBrush AccentGreen = new(System.Windows.Media.Color.FromRgb(0, 229, 80));
    private static readonly SolidColorBrush BorderNormal = new(System.Windows.Media.Color.FromRgb(42, 42, 42));
    private static readonly SolidColorBrush BorderHover = new(System.Windows.Media.Color.FromRgb(58, 58, 58));
    private static readonly SolidColorBrush BgCard = new(System.Windows.Media.Color.FromRgb(22, 22, 22));
    private static readonly SolidColorBrush TextWhite = new(System.Windows.Media.Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush TextDim = new(System.Windows.Media.Color.FromRgb(136, 136, 136));

    public int RateHz
    {
        get => (int)GetValue(RateHzProperty);
        set => SetValue(RateHzProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public event RoutedEventHandler RateSelected
    {
        add => AddHandler(RateSelectedEvent, value);
        remove => RemoveHandler(RateSelectedEvent, value);
    }

    public RateButton()
    {
        Background = BgCard;
        BorderBrush = BorderNormal;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        Width = 120;
        Height = 44;
        Cursor = Cursors.Hand;

        _label = new TextBlock
        {
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            Foreground = TextDim,
        };

        Child = _label;
        UpdateVisuals();

        MouseEnter += (_, _) => { if (!IsSelected) BorderBrush = BorderHover; };
        MouseLeave += (_, _) => { if (!IsSelected) BorderBrush = BorderNormal; };
        MouseLeftButtonDown += (_, _) => RaiseEvent(new RoutedEventArgs(RateSelectedEvent));
    }

    private static void OnRateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RateButton)d).UpdateVisuals();

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RateButton)d).UpdateVisuals();

    private void UpdateVisuals()
    {
        _label.Text = $"{RateHz:N0}Hz";
        if (IsSelected)
        {
            BorderBrush = AccentGreen;
            _label.Foreground = TextWhite;
            _label.FontWeight = FontWeights.Bold;
        }
        else
        {
            BorderBrush = BorderNormal;
            _label.Foreground = TextDim;
            _label.FontWeight = FontWeights.Normal;
        }
    }
}
