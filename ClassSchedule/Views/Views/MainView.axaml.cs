using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using ClassSchedule.ViewModels;

namespace ClassSchedule.Views;

public partial class MainView : UserControl
{
    private MainViewModel? _viewModel;
    private IDataTemplate? _courseCardTemplate;

    public MainView()
    {
        InitializeComponent();
        _courseCardTemplate = Resources["CourseCardTemplate"] as IDataTemplate;
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        // 主题切换后按新主题重建单元格配色。
        RebuildSchedule();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Days.CollectionChanged -= OnDaysCollectionChanged;
        }

        _viewModel = DataContext as MainViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Days.CollectionChanged += OnDaysCollectionChanged;
        RebuildSchedule();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.HasSchedule))
        {
            RebuildSchedule();
        }
    }

    private void OnDaysCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildSchedule();
    }

    /// <summary>根据 ViewModel 数据重新构建课表网格（节次行数、表头、节次列、每日课程卡片）。</summary>
    private void RebuildSchedule()
    {
        if (_viewModel is null)
        {
            return;
        }

        ScheduleRoot.Children.Clear();
        ScheduleRoot.RowDefinitions.Clear();

        var maxSection = Math.Max(1, _viewModel.MaxSection);

        // 第 0 行为表头，其余每节一行（星号等高）。
        ScheduleRoot.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var i = 0; i < maxSection; i++)
        {
            ScheduleRoot.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
        }

        // 左上角 + 星期表头。
        ScheduleRoot.Children.Add(MakeCell(0, 0, "节次", true, rightBorder: true, bottomBorder: true));
        for (var d = 0; d < _viewModel.Days.Count; d++)
        {
            var isLast = d == _viewModel.Days.Count - 1;
            ScheduleRoot.Children.Add(MakeCell(0, d + 1, _viewModel.Days[d].DayName, true, rightBorder: !isLast, bottomBorder: true));
        }

        // 节次列。
        for (var s = 0; s < maxSection; s++)
        {
            var label = s < _viewModel.SectionLabels.Count ? _viewModel.SectionLabels[s] : $"第{s + 1}节";
            ScheduleRoot.Children.Add(MakeCell(s + 1, 0, label, false, rightBorder: true, bottomBorder: true));
        }

        // 每日课程列（嵌套网格，支持跨节与重叠轨道）。
        for (var d = 0; d < _viewModel.Days.Count; d++)
        {
            var day = _viewModel.Days[d];
            var trackCount = Math.Max(1, day.TrackCount);

            var dayGrid = new Grid();
            for (var i = 0; i < maxSection; i++)
            {
                dayGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            }

            for (var t = 0; t < trackCount; t++)
            {
                dayGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            }

            Grid.SetRow(dayGrid, 1);
            Grid.SetColumn(dayGrid, d + 1);
            Grid.SetRowSpan(dayGrid, maxSection);
            Grid.SetColumnSpan(dayGrid, 1);

            foreach (var item in day.Items)
            {
                if (item.IsGridLine)
                {
                    var line = new Border
                    {
                        BorderBrush = GetBrush("GridLineBrush"),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                    };
                    Grid.SetRow(line, item.Row);
                    Grid.SetColumn(line, 0);
                    Grid.SetRowSpan(line, 1);
                    Grid.SetColumnSpan(line, trackCount);
                    dayGrid.Children.Add(line);
                }
                else if (_courseCardTemplate is not null)
                {
                    var card = _courseCardTemplate.Build(item);
                    if (card is not null)
                    {
                        Grid.SetRow(card, item.Row);
                        Grid.SetColumn(card, item.Column);
                        Grid.SetRowSpan(card, item.RowSpan);
                        Grid.SetColumnSpan(card, 1);
                        dayGrid.Children.Add(card);
                    }
                }
            }

            ScheduleRoot.Children.Add(dayGrid);
        }
    }

    /// <summary>构建一个课表单元格（表头或节次标签）。</summary>
    private Border MakeCell(int row, int column, string text, bool header,
        bool rightBorder, bool bottomBorder)
    {
        var border = new Border
        {
            Background = GetBrush(header ? "HeaderBackgroundBrush" : "CellBackgroundBrush"),
            BorderBrush = GetBrush("GridLineBrush"),
            BorderThickness = new Thickness(0, 0, rightBorder ? 1 : 0, bottomBorder ? 1 : 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = header ? 13 : 11,
                FontWeight = header ? FontWeight.SemiBold : FontWeight.Normal,
                Foreground = GetBrush(header ? "TextPrimaryBrush" : "TextSecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, header ? 8 : 2),
            },
        };

        Grid.SetRow(border, row);
        Grid.SetColumn(border, column);
        return border;
    }

    private IBrush? GetBrush(string key)
    {
        var theme = ActualThemeVariant;
        if (theme == ThemeVariant.Default)
        {
            theme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Light;
        }

        return TryGetResource(key, theme, out var value) ? value as IBrush : null;
    }
}