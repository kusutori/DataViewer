using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataViewer.Controls;
using DataViewer.Services;
using DataViewer.State;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinUI.TableView;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.RegisterControlAssembly(typeof(TableView).Assembly);
ReactorApp.Run<App>("DataViewer", 1180, 760, false, host =>
{
    ResultTableView.Register(host.Reconciler);
    SqlCodeEditor.Register(host.Reconciler);
});

class App : Component
{
    private static readonly DuckDbQueryEngine QueryEngine = new();
    private static string CurrentEditorSql = "";
    private static readonly string[] EditorFonts = ["Cascadia Code", "Consolas", "JetBrains Mono", "Courier New"];

    public override Element Render()
    {
        var (state, dispatch) = UseReducer<AppState, AppAction>(AppReducer.Reduce, AppState.Initial);
        var window = UseWindow();
        var nav = UseNavigation(AppRoute.Query);

        return FlexColumn(
            TitleBar("DataViewer") with { Subtitle = "本地数据读取与 SQL 查询原型" },
            (NavigationView(
                    [
                        NavItem("数据查询", icon: "\uE8A5", tag: ToTag(AppRoute.Query)),
                    ],
                    NavigationHost(nav, route => route switch
                    {
                        AppRoute.Query => RenderQueryPage(state, dispatch, window),
                        AppRoute.Settings => RenderSettingsPage(state, dispatch),
                        _ => TextBlock("页面不存在").Padding(24),
                    })
                )
                .WithNavigation(nav, ToTag, ToRoute)
                .PaneTitle("DataViewer")
                .PaneFooter(
                    Button(
                        HStack(12,
                            Icon(FontIcon("\uE713")),
                            TextBlock("设置")),
                        () => nav.Navigate(AppRoute.Settings))
                        .SubtleButton()
                        .AutomationName("打开设置"))
                .PaneDisplayMode(NavigationViewPaneDisplayMode.LeftCompact)
                with { IsSettingsVisible = false })
            .Flex(grow: 1, basis: 0)
        )
        .RequestedTheme(ToElementTheme(state.ThemeMode))
        .Backdrop(BackdropKind.Mica);
    }

    private static Element RenderQueryPage(AppState state, Action<AppAction> dispatch, ReactorWindow? window) =>
        Border(
            FlexRow(
                RenderDataPanel(state, dispatch, window),
                RenderWorkspace(state, dispatch)
            ) with { ColumnGap = 16 }
        )
        .Padding(16)
        .Flex(grow: 1, basis: 0);

    private static Element RenderDataPanel(AppState state, Action<AppAction> dispatch, ReactorWindow? window) =>
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
                        Caption(".tsv  使用制表符分隔读取"),
                        Caption(".parquet  使用 read_parquet 读取列式数据"),
                        Caption(".json / .jsonl / .ndjson  使用 read_json_auto"),
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
                Caption($"列数: {dataSet.Columns.Count}"),
                Caption($"预览上限: {dataSet.PreviewLimit} 行").Foreground(Theme.SecondaryText)
            )
        );
    }

    private static Element RenderWorkspace(AppState state, Action<AppAction> dispatch) =>
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

    private static Element RenderSqlPanel(AppState state, Action<AppAction> dispatch) =>
        Card(
            FlexColumn(
                HStack(8,
                    VStack(2,
                        Subtitle("SQL 查询"),
                        Caption("查询当前加载的数据集视图，默认限制 200 行。").Foreground(Theme.SecondaryText)
                    ).Flex(grow: 1, basis: 0),
                    Button("重置查询", () => ResetQuery(dispatch, state.DataSet))
                        .IsEnabled(!state.IsBusy && state.DataSet is not null),
                    Button("执行查询", () => RunQuery(dispatch, CurrentEditorSql))
                        .AccentButton()
                        .IsEnabled(!state.IsBusy && !string.IsNullOrWhiteSpace(state.SqlText))
                ),
                SqlCodeEditor.Render(
                    CurrentEditorSql,
                    state.DataSet,
                    state.ThemeMode,
                    state.EditorSettings,
                    state.SqlEditorSyncVersion,
                    value =>
                    {
                        CurrentEditorSql = value;
                    },
                    value =>
                    {
                        CurrentEditorSql = value;
                        RunQuery(dispatch, value);
                    })
                    .MinHeight(132)
                    .AutomationName("SQL 查询编辑器"),
                Caption("支持 SQL 高亮、关键字/表名/列名补全，按 Ctrl+Enter 执行。").Foreground(Theme.SecondaryText)
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
                ResultTableView.Render(state.Result)
                    .Flex(grow: 1, basis: 0)
            ) with { RowGap = 12 }
        )
        .Flex(grow: 1, basis: 0);

    private static Element RenderSettingsPage(AppState state, Action<AppAction> dispatch) =>
        Border(
            FlexColumn(
                VStack(4,
                    Heading("设置"),
                    Caption("调整应用外观和 SQL 编辑器行为。").Foreground(Theme.SecondaryText)
                ),
                Card(
                    VStack(12,
                        TextBlock("外观").SemiBold(),
                        ComboBox(
                            ["跟随系统", "浅色", "深色"],
                            ThemeIndex(state.ThemeMode),
                            index => dispatch(new ThemeChanged(ThemeFromIndex(index))))
                            .Header("应用主题")
                    )
                ),
                Card(
                    VStack(12,
                        TextBlock("SQL 编辑器").SemiBold(),
                        ComboBox(
                            ["跟随应用", "浅色", "深色"],
                            EditorThemeIndex(state.EditorSettings.ThemeMode),
                            index => dispatch(new EditorThemeChanged(EditorThemeFromIndex(index))))
                            .Header("编辑器配色"),
                        ComboBox(
                            EditorFonts,
                            EditorFontIndex(state.EditorSettings.FontFamily),
                            index => dispatch(new EditorFontFamilyChanged(EditorFonts[index])))
                            .Header("字体"),
                        NumberBox(
                            state.EditorSettings.FontSize,
                            value => dispatch(new EditorFontSizeChanged(value)),
                            header: "字号")
                            .Range(11, 22)
                            .SpinButtons(NumberBoxSpinButtonPlacementMode.Inline),
                        ToggleSwitch(
                            state.EditorSettings.AcceptCompletionOnTab,
                            value => dispatch(new EditorAcceptCompletionOnTabChanged(value)),
                            onContent: "Tab 接受补全",
                            offContent: "Tab 保持默认焦点行为",
                            header: "补全行为")
                    )
                )
            ) with { RowGap = 16 }
        )
        .Padding(24)
        .Flex(grow: 1, basis: 0);

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
        picker.FileTypeFilter.Add(".tsv");
        picker.FileTypeFilter.Add(".parquet");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".jsonl");
        picker.FileTypeFilter.Add(".ndjson");
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
            CurrentEditorSql = sql;
            dispatch(new FileLoaded(dataSet, sql, result));
        }
        catch (Exception ex)
        {
            dispatch(new LoadFailed(ex.Message));
        }
    }

    private static void RunQuery(Action<AppAction> dispatch, string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            dispatch(new QueryFailed("请输入 SQL 后再执行。"));
            return;
        }

        CurrentEditorSql = sql;
        dispatch(new SqlChanged(sql));
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
            CurrentEditorSql = sql;
            dispatch(new ResetQuery(sql, result));
        }
        catch (Exception ex)
        {
            dispatch(new QueryFailed(ex.Message));
        }
    }

    private static int ThemeIndex(ThemeMode mode) =>
        mode switch
        {
            ThemeMode.Light => 1,
            ThemeMode.Dark => 2,
            _ => 0,
        };

    private static ThemeMode ThemeFromIndex(int index) =>
        index switch
        {
            1 => ThemeMode.Light,
            2 => ThemeMode.Dark,
            _ => ThemeMode.System,
        };

    private static int EditorThemeIndex(CodeEditorThemeMode mode) =>
        mode switch
        {
            CodeEditorThemeMode.Light => 1,
            CodeEditorThemeMode.Dark => 2,
            _ => 0,
        };

    private static CodeEditorThemeMode EditorThemeFromIndex(int index) =>
        index switch
        {
            1 => CodeEditorThemeMode.Light,
            2 => CodeEditorThemeMode.Dark,
            _ => CodeEditorThemeMode.FollowApp,
        };

    private static int EditorFontIndex(string fontFamily)
    {
        var index = Array.IndexOf(EditorFonts, fontFamily);
        return index < 0 ? 0 : index;
    }

    private static string ToTag(AppRoute route) =>
        route.ToString().ToLowerInvariant();

    private static AppRoute ToRoute(string tag) =>
        Enum.Parse<AppRoute>(tag, ignoreCase: true);

    private static ElementTheme ToElementTheme(ThemeMode mode) =>
        mode switch
        {
            ThemeMode.Light => ElementTheme.Light,
            ThemeMode.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
}

enum AppRoute
{
    Query,
    Settings,
}
