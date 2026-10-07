using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinClean.ViewModels;

namespace WinClean.Views;

public partial class ProcessesPage : UserControl
{
    private const double MinimumNameColumnWidth = 240;

    private const double ScrollBarAllowance = 28;

    private ProcessesViewModel? _viewModel;

    private bool _menuOpen;

    public ProcessesPage()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private GridView Columns => (GridView)Table.View;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = e.NewValue as ProcessesViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        UpdateGpuColumn();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProcessesViewModel.HasGpu))
        {
            UpdateGpuColumn();
        }
    }

    /// <summary>The GPU column only exists on machines that report GPU counters.</summary>
    private void UpdateGpuColumn()
    {
        var hasGpu = _viewModel?.HasGpu ?? false;
        var present = Columns.Columns.Contains(GpuColumn);

        if (hasGpu && !present)
        {
            Columns.Columns.Add(GpuColumn);
        }
        else if (!hasGpu && present)
        {
            Columns.Columns.Remove(GpuColumn);
        }

        UpdateNameColumnWidth();
    }

    /// <summary>GridView has no star sizing, so the name column takes whatever the fixed columns leave.</summary>
    private void UpdateNameColumnWidth()
    {
        var others = 0.0;

        foreach (var column in Columns.Columns)
        {
            if (!ReferenceEquals(column, NameColumn))
            {
                others += column.Width;
            }
        }

        NameColumn.Width = Math.Max(MinimumNameColumnWidth, Table.ActualWidth - others - ScrollBarAllowance);
    }

    private void OnTableSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            UpdateNameColumnWidth();
        }
    }

    private void OnHeaderClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is GridViewColumnHeader { Tag: string tag } && Enum.TryParse<ProcessColumn>(tag, out var column))
        {
            _viewModel?.SortBy(column);
        }
    }

    private void OnTablePreviewMouseDown(object sender, MouseButtonEventArgs e) => _viewModel?.HoldOrder(true);

    private void OnTablePreviewMouseUp(object sender, MouseButtonEventArgs e) => ReleaseOrder();

    private void OnTableMouseLeave(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
        {
            ReleaseOrder();
        }
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (_viewModel?.SelectedRow is null)
        {
            e.Handled = true;
            return;
        }

        _menuOpen = true;
        _viewModel.HoldOrder(true);
    }

    private void OnContextMenuClosed(object sender, RoutedEventArgs e)
    {
        _menuOpen = false;
        ReleaseOrder();
    }

    private void ReleaseOrder()
    {
        if (!_menuOpen)
        {
            _viewModel?.HoldOrder(false);
        }
    }

    private void OnTableDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel?.SelectedRow is not { } row || e.OriginalSource is not DependencyObject source || !IsInsideRow(source))
        {
            return;
        }

        if (row.IsGroup)
        {
            _viewModel.ToggleExpandCommand.Execute(row);
        }
        else
        {
            _viewModel.ToggleDetailsCommand.Execute(null);
        }
    }

    private void OnTableKeyDown(object sender, KeyEventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        var row = _viewModel.SelectedRow;

        switch (e.Key)
        {
            case Key.Right when row is { IsGroup: true, IsExpanded: false }:
            case Key.Left when row is { IsGroup: true, IsExpanded: true }:
            case Key.Space when row is { IsGroup: true }:
                _viewModel.ToggleExpandCommand.Execute(row);
                e.Handled = true;
                break;

            case Key.Return when row is { IsGroup: true } && Keyboard.Modifiers == ModifierKeys.None:
                _viewModel.ToggleExpandCommand.Execute(row);
                e.Handled = true;
                break;

            case Key.Escape when _viewModel.IsDetailsOpen:
                _viewModel.IsDetailsOpen = false;
                e.Handled = true;
                break;

            case Key.Escape when _viewModel.SearchText.Length > 0:
                _viewModel.ClearSearchCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            Search.FocusInput();
            e.Handled = true;
        }
    }

    private static bool IsInsideRow(DependencyObject source)
    {
        return ItemsControl.ContainerFromElement(null, source) is ListViewItem;
    }
}
