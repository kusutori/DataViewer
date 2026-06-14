using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataViewer.Services;
using DataViewer.State;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<App>("DataViewer", width: 1180, height: 760);

class App : Component
{
    private static readonly DuckDbQueryEngine QueryEngine = new();

    public override Element Render()
    {
        var (state, dispatch) = UseReducer<AppState, AppAction>(AppReducer.Reduce, AppState.Initial);
        var window = UseWindow();

        return FlexColumn(
            TitleBar("DataViewer") with { Subtitle = "本地数据读取与 SQL 查询原型" },
            Border(
                FlexRow(
                    RenderSidebar(state, dispatch, window),
                    RenderWorkspace(state, dispatch)
                ) with { ColumnGap = 16 }
            )
            .Padding(16)
            .Flex(grow: 1, basis: 0)
        ).Backdrop(BackdropKind.Mica);
    }

    private static Element RenderSidebar(AppState state, Action<AppAction> dispatch, ReactorWindow? window) =>
        Border(
            FlexColumn(
                VStack(4,
                    Subtitle("数据集"),
                    Caption("读取 CSV / Parquet 后，可直接用 SQL 查询。").Foreground(Theme.SecondaryText)
                ),
                Button("打开文件", async () => await OpenFileAsync(dispatch, window))
                    .AccentButton()
                    .IsEnabled(!state.IsBusy)
                    .AutomationName("打开数据集文件"),
                RenderCurrentDataSet(state.DataSet),
                Card(
                    VStack(8,
                        TextBlock("支持格式").SemiBold(),
                        Caption(".csv  使用 read_csv_auto 自动识别"),
                        Caption(".parquet  使用 read_parquet 读取列式数据"),
                        Caption("后续格式通过 IDataSetReader 扩展。").Foreground(Theme.SecondaryText)
                    )
                ),
                state.IsBusy
                    ? Card(HStack(8, ProgressRing().IsActive(), TextBlock("正在处理...")))
                    : null
            ) with { RowGap = 16 }
        )
        .Padding(16)
        .Background(Theme.CardBackground)
        .WithBorder(Theme.CardStroke, 1)
        .CornerRadius(8)
        .Flex(shrink: 0, basis: 280);

    private static Element RenderCurrentDataSet(LoadedDataSet? dataSet)
    {
        if (dataSet is null)
        {
            return Card(
                VStack(8,
                    TextBlock("尚未加载文件").SemiBold(),
                    Caption("请选择一个本地 CSV 或 Parquet 文件开始。").Foreground(Theme.SecondaryText)
                )
            );
        }

        return Card(
            VStack(8,
                TextBlock(dataSet.FileName).SemiBold().TextTrimming(TextTrimming.CharacterEllipsis).ToolTip(dataSet.Path),
                Caption($"格式: {dataSet.Format}"),
                Caption($"SQL 别名: {dataSet.Alias}"),
                Caption($"预览上限: {dataSet.PreviewLimit} 行").Foreground(Theme.SecondaryText)
            )
        );
    }

    private Element RenderWorkspace(AppState state, Action<AppAction> dispatch) =>
        (FlexColumn(
            RenderToast(state, dispatch),
            RenderSqlPanel(state, dispatch),
            RenderResultPanel(state)
        ) with { RowGap = 16 })
        .Flex(grow: 1, basis: 0);

    private static Element? RenderToast(AppState state, Action<AppAction> dispatch)
    {
        if (state.ErrorMessage is null)
        {
            return null;
        }

        return InfoBar("SQL 执行失败", state.ErrorMessage)
            .Severity(InfoBarSeverity.Error)
            .IsClosable()
            .Closed(() => dispatch(new DismissToast()));
    }

    private Element RenderSqlPanel(AppState state, Action<AppAction> dispatch) =>
        Card(
            FlexColumn(
                HStack(8,
                    VStack(2,
                        Subtitle("SQL 查询"),
                        Caption("查询当前加载的数据集视图，默认限制 200 行。").Foreground(Theme.SecondaryText)
                    ).Flex(grow: 1, basis: 0),
                    Button("重置查询", () => ResetQuery(dispatch, state.DataSet))
                        .IsEnabled(!state.IsBusy && state.DataSet is not null),
                    Button("执行查询", () => RunQuery(dispatch, state.SqlText))
                        .AccentButton()
                        .IsEnabled(!state.IsBusy && !string.IsNullOrWhiteSpace(state.SqlText))
                ),
                TextBox(state.SqlText, value => dispatch(new SqlChanged(value)), placeholderText: "SELECT * FROM dataset_1 LIMIT 200;")
                    .AcceptsReturn()
                    .TextWrapping(TextWrapping.NoWrap)
                    .MinHeight(116)
                    .AutomationName("SQL 查询编辑器")
            ) with { RowGap = 12 }
        );

    private static Element RenderResultPanel(AppState state) =>
        Card(
            FlexColumn(
                HStack(8,
                    VStack(2,
                        Subtitle("查询结果"),
                        Caption(state.Result is null
                            ? "加载数据或执行 SQL 后显示结果。"
                            : $"{state.Result.DisplayedRowCount} 行，{state.Result.Columns.Count} 列").Foreground(Theme.SecondaryText)
                    ).Flex(grow: 1, basis: 0),
                    state.IsBusy ? ProgressRing().IsActive() : null
                ),
                RenderResultTable(state.Result)
            ) with { RowGap = 12 }
        )
        .Flex(grow: 1, basis: 0);

    private static Element RenderResultTable(QueryResult? result)
    {
        if (result is null)
        {
            return Border(
                VStack(8,
                    TextBlock("暂无结果").SemiBold(),
                    Caption("打开数据文件后会自动执行默认查询。").Foreground(Theme.SecondaryText)
                )
            )
            .Padding(24)
            .Background(Theme.SubtleFill)
            .CornerRadius(8)
            .Flex(grow: 1, basis: 0);
        }

        if (result.Columns.Count == 0)
        {
            return Border(TextBlock("查询已执行，但没有返回表格列。"))
                .Padding(24)
                .Background(Theme.SubtleFill)
                .CornerRadius(8)
                .Flex(grow: 1, basis: 0);
        }

        var visibleColumnCount = Math.Min(result.Columns.Count, 24);
        var visibleRows = result.Rows.Take(200).ToArray();
        var columns = Enumerable.Repeat(GridSize.Px(180), visibleColumnCount).ToArray();
        var rows = Enumerable.Repeat(GridSize.Auto, visibleRows.Length + 1).ToArray();
        var cells = new List<Element>();

        for (var column = 0; column < visibleColumnCount; column++)
        {
            cells.Add(RenderCell(result.Columns[column], isHeader: true).Grid(row: 0, column: column));
        }

        for (var row = 0; row < visibleRows.Length; row++)
        {
            for (var column = 0; column < visibleColumnCount; column++)
            {
                var value = column < visibleRows[row].Count ? visibleRows[row][column] : null;
                cells.Add(RenderCell(value ?? "NULL", isHeader: false, isNull: value is null).Grid(row: row + 1, column: column));
            }
        }

        return ScrollView(
            Grid(columns, rows, cells.ToArray()) with { RowSpacing = 1, ColumnSpacing = 1 }
        )
        .HorizontalScrollMode(ScrollingScrollMode.Enabled)
        .VerticalScrollMode(ScrollingScrollMode.Enabled)
        .Flex(grow: 1, basis: 0);
    }

    private static Element RenderCell(string text, bool isHeader, bool isNull = false)
    {
        var content = TextBlock(text)
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .ToolTip(text)
            .Foreground(isNull ? Theme.TertiaryText : Theme.PrimaryText);

        if (isHeader)
        {
            content = content.SemiBold();
        }

        return Border(content)
            .Padding(horizontal: 8, vertical: 6)
            .MinHeight(36)
            .Background(isHeader ? Theme.LayerFill : Theme.CardBackground)
            .WithBorder(Theme.DividerStroke, 1);
    }

    private static async Task OpenFileAsync(Action<AppAction> dispatch, ReactorWindow? window)
    {
        try
        {
            if (window is null)
            {
                dispatch(new LoadFailed("窗口尚未就绪，无法打开文件选择器。"));
                return;
            }

            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = "打开",
            };
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".parquet");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window.NativeWindow));

            var file = await picker.PickSingleFileAsync();

            if (file is null)
            {
                return;
            }

            dispatch(new OpenFileRequested());

            var dataSet = QueryEngine.LoadDataSet(file.Path);
            var sql = QueryEngine.CreateDefaultSql(dataSet);
            var result = QueryEngine.Query(sql);
            dispatch(new FileLoaded(dataSet, sql, result));
        }
        catch (Exception ex)
        {
            dispatch(new LoadFailed(ex.Message));
        }
    }

    private static void RunQuery(Action<AppAction> dispatch, string sql)
    {
        dispatch(new RunQueryRequested());

        try
        {
            var result = QueryEngine.Query(sql);
            dispatch(new QuerySucceeded(result));
        }
        catch (Exception ex)
        {
            dispatch(new QueryFailed(ex.Message));
        }
    }

    private static void ResetQuery(Action<AppAction> dispatch, LoadedDataSet? dataSet)
    {
        if (dataSet is null)
        {
            return;
        }

        dispatch(new RunQueryRequested());

        try
        {
            var sql = QueryEngine.CreateDefaultSql(dataSet);
            var result = QueryEngine.Query(sql);
            dispatch(new ResetQuery(sql, result));
        }
        catch (Exception ex)
        {
            dispatch(new QueryFailed(ex.Message));
        }
    }
}
