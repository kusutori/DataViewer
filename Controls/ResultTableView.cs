using System.Collections.Generic;
using System.Linq;
using DataViewer.Services;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI.TableView;

namespace DataViewer.Controls;

public static class ResultTableView
{
    public static void Register(Reconciler reconciler)
    {
        reconciler.RegisterType<ResultTableViewElement, TableView>(
            mount: (_, element, _) =>
            {
                var table = new TableView();
                ApplyOptions(table, element);
                ApplyResult(table, element);
                return table;
            },
            update: (_, oldElement, newElement, table, _) =>
            {
                ApplyOptions(table, newElement);

                if (oldElement != newElement)
                {
                    ApplyResult(table, newElement);
                }

                return table;
            },
            unmount: (_, table) =>
            {
                table.ItemsSource = null;
                table.Columns.Clear();
            });
    }

    public static ResultTableViewElement Render(QueryResult? result) =>
        new ResultTableViewElement(result);

    private static void ApplyOptions(TableView table, ResultTableViewElement element)
    {
        table.AutoGenerateColumns = false;
        table.CanFilterColumns = element.CanFilter;
        table.CanResizeColumns = element.CanResizeColumns;
        table.CanReorderColumns = element.CanReorderColumns;
        table.CanSortColumns = element.CanSort;
        table.IsReadOnly = element.IsReadOnly;
        table.SelectionMode = element.SelectionMode;
        table.ShowExportOptions = element.ShowExportOptions;
        table.GridLinesVisibility = element.GridLinesVisibility;
        table.HeaderGridLinesVisibility = element.HeaderGridLinesVisibility;
        table.RowMinHeight = element.RowMinHeight;
        table.MinColumnWidth = element.MinColumnWidth;
        table.MaxColumnWidth = element.MaxColumnWidth;
    }

    private static void ApplyResult(TableView table, ResultTableViewElement element)
    {
        var result = element.Result;
        table.Columns.Clear();

        if (result is null || result.Columns.Count == 0)
        {
            table.ItemsSource = null;
            return;
        }

        for (var i = 0; i < result.Columns.Count; i++)
        {
            var key = $"c{i}";
            table.Columns.Add(new TableViewTextColumn
            {
                Header = result.Columns[i],
                Binding = new Binding
                {
                    Path = new PropertyPath($"[{key}]"),
                    Mode = BindingMode.OneWay,
                },
                ClipboardContentBinding = new Binding
                {
                    Path = new PropertyPath($"[{key}]"),
                    Mode = BindingMode.OneWay,
                },
                Width = new GridLength(element.ColumnWidth),
                CanFilter = element.CanFilter,
                CanSort = element.CanSort,
                IsReadOnly = element.IsReadOnly,
            });
        }

        table.ItemsSource = result.Rows
            .Select(row =>
            {
                var item = new Dictionary<string, string?>();
                for (var i = 0; i < result.Columns.Count; i++)
                {
                    item[$"c{i}"] = i < row.Count ? row[i] : null;
                }

                return item;
            })
            .ToArray();
    }
}

public record ResultTableViewElement(QueryResult? Result) : Element
{
    public bool CanSort { get; init; } = true;
    public bool CanFilter { get; init; } = true;
    public bool CanResizeColumns { get; init; } = true;
    public bool CanReorderColumns { get; init; } = true;
    public bool IsReadOnly { get; init; } = true;
    public bool ShowExportOptions { get; init; } = true;
    public double RowMinHeight { get; init; } = 36;
    public double ColumnWidth { get; init; } = 180;
    public double MinColumnWidth { get; init; } = 96;
    public double MaxColumnWidth { get; init; } = 480;
    public ListViewSelectionMode SelectionMode { get; init; } = ListViewSelectionMode.Extended;
    public TableViewGridLinesVisibility GridLinesVisibility { get; init; } = TableViewGridLinesVisibility.All;
    public TableViewGridLinesVisibility HeaderGridLinesVisibility { get; init; } = TableViewGridLinesVisibility.All;
}
