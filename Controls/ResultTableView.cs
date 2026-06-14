using System.Collections.Generic;
using System.Linq;
using DataViewer.Services;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI.TableView;

namespace DataViewer.Controls;

public static class ResultTableView
{
    public static Element Render(QueryResult? result) =>
        new XamlHostElement(
            Factory: () =>
            {
                var table = new TableView
                {
                    AutoGenerateColumns = false,
                    CanFilterColumns = true,
                    CanResizeColumns = true,
                    CanReorderColumns = true,
                    CanSortColumns = true,
                    IsReadOnly = true,
                    SelectionMode = ListViewSelectionMode.Extended,
                    ShowExportOptions = true,
                    GridLinesVisibility = TableViewGridLinesVisibility.All,
                    HeaderGridLinesVisibility = TableViewGridLinesVisibility.All,
                    RowMinHeight = 36,
                    MinColumnWidth = 96,
                    MaxColumnWidth = 480,
                };

                ApplyResult(table, result);
                return table;
            },
            Updater: element =>
            {
                if (element is TableView table)
                {
                    ApplyResult(table, result);
                }
            });

    private static void ApplyResult(TableView table, QueryResult? result)
    {
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
                Width = new GridLength(180),
                CanFilter = true,
                CanSort = true,
                IsReadOnly = true,
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
