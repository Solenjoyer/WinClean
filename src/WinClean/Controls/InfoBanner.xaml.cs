using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinClean.ViewModels;

namespace WinClean.Controls;

/// <summary>One line of plain text with a status glyph, shown at the top of a page until dismissed.</summary>
public partial class InfoBanner : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(InfoBanner), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(
        nameof(Severity), typeof(BannerSeverity), typeof(InfoBanner), new PropertyMetadata(BannerSeverity.Information, OnSeverityChanged));

    public static readonly DependencyProperty DismissCommandProperty = DependencyProperty.Register(
        nameof(DismissCommand), typeof(ICommand), typeof(InfoBanner), new PropertyMetadata(null));

    public InfoBanner()
    {
        InitializeComponent();
        ApplySeverity();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public BannerSeverity Severity
    {
        get => (BannerSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public ICommand? DismissCommand
    {
        get => (ICommand?)GetValue(DismissCommandProperty);
        set => SetValue(DismissCommandProperty, value);
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((InfoBanner)d).ApplySeverity();

    private void ApplySeverity()
    {
        var (glyph, brushKey) = Severity switch
        {
            BannerSeverity.Warning => (Glyphs.Warning, "SystemFillColorCautionBrush"),
            BannerSeverity.Error => (Glyphs.StatusError, "SystemFillColorCriticalBrush"),
            _ => (Glyphs.StatusInfo, "TextFillColorSecondaryBrush"),
        };

        Glyph.Text = glyph;
        Glyph.SetResourceReference(ForegroundProperty, brushKey);
    }
}
