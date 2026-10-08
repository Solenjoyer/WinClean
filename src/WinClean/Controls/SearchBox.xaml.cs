using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WinClean.Controls;

/// <summary>A text box with a search glyph, a placeholder and a clear button; Escape clears it.</summary>
public partial class SearchBox : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(SearchBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

    public SearchBox()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public void FocusInput()
    {
        Input.Focus();
        Input.SelectAll();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Key == Key.Escape && Text.Length > 0)
        {
            Text = string.Empty;
            e.Handled = true;
        }
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        Text = string.Empty;
        Input.Focus();
    }
}
