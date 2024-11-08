using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace kv4p_net8_app.Behavior;

public static class FontSizeAutoFitBehavior
{
    public static readonly DependencyProperty EnableAutoFitProperty =
        DependencyProperty.RegisterAttached(
            "EnableAutoFit",
            typeof(bool),
            typeof(FontSizeAutoFitBehavior),
            new PropertyMetadata(false, OnEnableAutoFitChanged)
        );

    public static bool GetEnableAutoFit(TextBox textBox) =>
        (bool)textBox.GetValue(EnableAutoFitProperty);

    public static void SetEnableAutoFit(TextBox textBox, bool value) =>
        textBox.SetValue(EnableAutoFitProperty, value);

    private static void OnEnableAutoFitChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is TextBox textBox)
        {
            if ((bool)e.NewValue)
            {
                textBox.SizeChanged += TextBox_SizeChanged;
                textBox.TextChanged += TextBox_TextChanged;
                UpdateFontSize(textBox);
            }
            else
            {
                textBox.SizeChanged -= TextBox_SizeChanged;
                textBox.TextChanged -= TextBox_TextChanged;
            }
        }
    }

    private static void TextBox_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdateFontSize(textBox);
        }
    }

    private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdateFontSize(textBox);
        }
    }

    private static void UpdateFontSize(TextBox textBox)
    {
        var initialFontSize = 12.0;
        var desiredWidth = textBox.ActualWidth - 10; // Adjust as needed
        var desiredHeight = textBox.ActualHeight - 10;

        if (desiredWidth <= 0 || desiredHeight <= 0)
            return;

        var formattedText = new System.Windows.Media.FormattedText(
            textBox.Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new System.Windows.Media.Typeface(
                textBox.FontFamily,
                textBox.FontStyle,
                textBox.FontWeight,
                textBox.FontStretch
            ),
            initialFontSize,
            textBox.Foreground,
            VisualTreeHelper.GetDpi(textBox).PixelsPerDip
        );

        double scalingFactor = Math.Min(
            desiredWidth / formattedText.Width,
            desiredHeight / formattedText.Height
        );
        textBox.FontSize = Math.Max(initialFontSize * scalingFactor, initialFontSize);
    }
}
