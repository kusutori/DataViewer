using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DataViewer.Services;
using DataViewer.State;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace DataViewer.Controls;

public static class SqlCodeEditor
{
    public static void Register(Reconciler reconciler)
    {
        reconciler.RegisterType<SqlCodeEditorElement, WebView2>(
            mount: (_, element, _) =>
            {
                var runtime = new SqlCodeEditorRuntime(element);
                var webView = new WebView2
                {
                    Tag = runtime,
                };

                webView.WebMessageReceived += OnWebMessageReceived;
                webView.NavigationCompleted += OnNavigationCompleted;
                _ = InitializeAsync(webView);

                return webView;
            },
            update: (_, oldElement, newElement, webView, _) =>
            {
                if (webView.Tag is SqlCodeEditorRuntime runtime)
                {
                    runtime.Element = newElement;
                }
                else
                {
                    webView.Tag = new SqlCodeEditorRuntime(newElement);
                }

                if (ShouldSendState(oldElement, newElement))
                {
                    _ = SendStateAsync(webView, newElement);
                }

                return webView;
            },
            unmount: (_, webView) =>
            {
                webView.WebMessageReceived -= OnWebMessageReceived;
                webView.NavigationCompleted -= OnNavigationCompleted;
                webView.Tag = null;
                webView.Close();
            });
    }

    public static SqlCodeEditorElement Render(
        string value,
        LoadedDataSet? dataSet,
        ThemeMode themeMode,
        CodeEditorSettings settings,
        int syncVersion,
        Action<string> onTextChanged,
        Action<string> onRunRequested) =>
        new(value, dataSet?.Alias, dataSet?.Columns ?? [], themeMode, settings, syncVersion, onTextChanged, onRunRequested);

    private static async Task InitializeAsync(WebView2 webView)
    {
        await webView.EnsureCoreWebView2Async();
        webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        webView.NavigateToString(EditorHtml);
    }

    private static void OnNavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        if (args.IsSuccess && sender.Tag is SqlCodeEditorRuntime runtime)
        {
            _ = SendStateAsync(sender, runtime.Element);
        }
    }

    private static void OnWebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        if (sender.Tag is not SqlCodeEditorRuntime runtime)
        {
            return;
        }

        var element = runtime.Element;

        try
        {
            using var document = JsonDocument.Parse(args.WebMessageAsJson);
            var root = document.RootElement;
            var type = root.GetProperty("type").GetString();
            var value = root.TryGetProperty("value", out var valueElement) ? valueElement.GetString() ?? "" : "";

            switch (type)
            {
                case "change":
                    element.OnTextChanged(value);
                    break;
                case "run":
                    element.OnRunRequested(value);
                    break;
                case "ready":
                    _ = SendStateAsync(sender, element);
                    break;
            }
        }
        catch
        {
            // Ignore malformed editor messages; the SQL runner owns user-facing errors.
        }
    }

    private static async Task SendStateAsync(WebView2 webView, SqlCodeEditorElement element)
    {
        if (webView.CoreWebView2 is null)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            value = element.Value,
            appTheme = element.AppThemeMode.ToString().ToLowerInvariant(),
            editorTheme = element.Settings.ThemeMode.ToString().ToLowerInvariant(),
            fontFamily = element.Settings.FontFamily,
            fontSize = element.Settings.FontSize,
            acceptCompletionOnTab = element.Settings.AcceptCompletionOnTab,
            tableName = element.TableName,
            columns = element.Columns,
        });

        try
        {
            await webView.ExecuteScriptAsync($"window.dataViewerSqlEditor?.setState({payload});");
        }
        catch
        {
            // The editor may still be navigating; NavigationCompleted will resend the state.
        }
    }

    private static bool ShouldSendState(SqlCodeEditorElement oldElement, SqlCodeEditorElement newElement) =>
        oldElement.SyncVersion != newElement.SyncVersion ||
        oldElement.AppThemeMode != newElement.AppThemeMode ||
        oldElement.Settings != newElement.Settings ||
        oldElement.TableName != newElement.TableName ||
        !oldElement.Columns.SequenceEqual(newElement.Columns);

    private const string EditorHtml = """
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta http-equiv="Content-Security-Policy" content="default-src 'self' https://esm.sh; script-src 'unsafe-inline' https://esm.sh; style-src 'unsafe-inline'; connect-src https://esm.sh; img-src data:;">
  <style>
    html, body, #editor {
      width: 100%;
      height: 100%;
      margin: 0;
      overflow: hidden;
    }

    body {
      background: transparent;
    }

    .cm-editor {
      height: 100%;
      outline: none;
    }

    .cm-focused {
      outline: none !important;
    }

    .cm-scroller {
      font-family: "Cascadia Code", "Consolas", monospace;
      font-size: 13px;
      line-height: 1.55;
    }

    .cm-content {
      padding: 12px 0;
    }

    .cm-gutters {
      border-right-width: 1px;
    }
  </style>
</head>
<body>
  <div id="editor"></div>
  <script type="importmap">
    {
      "imports": {
        "@codemirror/autocomplete": "https://esm.sh/@codemirror/autocomplete@6.18.7?external=@codemirror/state,@codemirror/view,@codemirror/language",
        "@codemirror/commands": "https://esm.sh/@codemirror/commands@6.8.1?external=@codemirror/state,@codemirror/view",
        "@codemirror/lang-sql": "https://esm.sh/@codemirror/lang-sql@6.10.0?external=@codemirror/state,@codemirror/view,@codemirror/language,@codemirror/autocomplete",
        "@codemirror/language": "https://esm.sh/@codemirror/language@6.11.3?external=@codemirror/state,@codemirror/view",
        "@codemirror/lint": "https://esm.sh/@codemirror/lint@6.8.5?external=@codemirror/state,@codemirror/view",
        "@codemirror/search": "https://esm.sh/@codemirror/search@6.5.11?external=@codemirror/state,@codemirror/view,@codemirror/language",
        "@codemirror/state": "https://esm.sh/@codemirror/state@6.5.2",
        "@codemirror/view": "https://esm.sh/@codemirror/view@6.38.0?external=@codemirror/state"
      }
    }
  </script>
  <script type="module">
    import {acceptCompletion, autocompletion, closeBrackets, closeBracketsKeymap, completionKeymap} from "@codemirror/autocomplete";
    import {defaultKeymap, history, historyKeymap} from "@codemirror/commands";
    import {sql, StandardSQL} from "@codemirror/lang-sql";
    import {bracketMatching, defaultHighlightStyle, foldGutter, foldKeymap, indentOnInput, syntaxHighlighting} from "@codemirror/language";
    import {lintKeymap} from "@codemirror/lint";
    import {highlightSelectionMatches, searchKeymap} from "@codemirror/search";
    import {Compartment} from "@codemirror/state";
    import {
      crosshairCursor,
      drawSelection,
      dropCursor,
      EditorView,
      highlightActiveLine,
      highlightActiveLineGutter,
      keymap,
      lineNumbers,
      rectangularSelection
    } from "@codemirror/view";

    const language = new Compartment();
    const theme = new Compartment();
    const tabCompletion = new Compartment();
    let currentState = {
      value: "",
      appTheme: "system",
      editorTheme: "followapp",
      fontFamily: "Cascadia Code",
      fontSize: 13,
      acceptCompletionOnTab: true,
      tableName: null,
      columns: []
    };
    let applyingRemoteChange = false;

    function post(message) {
      if (window.chrome?.webview) {
        window.chrome.webview.postMessage(message);
      }
    }

    function resolveDarkMode(mode) {
      if (mode === "dark") return true;
      if (mode === "light") return false;
      return window.matchMedia?.("(prefers-color-scheme: dark)")?.matches ?? false;
    }

    function schemaFromState(state) {
      if (!state.tableName) return {};
      return {
        [state.tableName]: state.columns.map(column => ({
          label: column,
          type: "property"
        }))
      };
    }

    function sqlExtension(state) {
      return sql({
        dialect: StandardSQL,
        schema: schemaFromState(state),
        defaultTable: state.tableName || undefined,
        upperCaseKeywords: true
      });
    }

    function editorTheme(state) {
      const requestedTheme = state.editorTheme === "followapp" ? state.appTheme : state.editorTheme;
      const isDark = resolveDarkMode(requestedTheme);
      return EditorView.theme({
        "&": {
          height: "100%",
          color: isDark ? "#f3f3f3" : "#1a1a1a",
          backgroundColor: isDark ? "#1e1e1e" : "#ffffff"
        },
        ".cm-scroller": {
          fontFamily: JSON.stringify(state.fontFamily) + ", Consolas, monospace",
          fontSize: `${state.fontSize}px`
        },
        ".cm-content": {
          caretColor: isDark ? "#ffffff" : "#111111"
        },
        ".cm-cursor, .cm-dropCursor": {
          borderLeftColor: isDark ? "#ffffff" : "#111111"
        },
        ".cm-gutters": {
          color: isDark ? "#9a9a9a" : "#696969",
          backgroundColor: isDark ? "#252526" : "#f7f7f7",
          borderRightColor: isDark ? "#3d3d3d" : "#e5e5e5"
        },
        ".cm-activeLine": {
          backgroundColor: isDark ? "#2a2d2e" : "#f3f7ff"
        },
        ".cm-activeLineGutter": {
          backgroundColor: isDark ? "#2a2d2e" : "#edf4ff"
        },
        ".cm-tooltip": {
          borderRadius: "8px",
          borderColor: isDark ? "#4a4a4a" : "#d0d0d0",
          backgroundColor: isDark ? "#252526" : "#ffffff",
          color: isDark ? "#f3f3f3" : "#1a1a1a"
        },
        ".cm-tooltip-autocomplete ul li[aria-selected]": {
          backgroundColor: isDark ? "#094771" : "#e5f1fb",
          color: isDark ? "#ffffff" : "#111111"
        }
      }, {dark: isDark});
    }

    function tabCompletionExtension(state) {
      return state.acceptCompletionOnTab
        ? keymap.of([{key: "Tab", run: acceptCompletion}])
        : [];
    }

    const view = new EditorView({
      doc: "",
      parent: document.getElementById("editor"),
      extensions: [
        lineNumbers(),
        highlightActiveLineGutter(),
        history(),
        foldGutter(),
        drawSelection(),
        dropCursor(),
        indentOnInput(),
        syntaxHighlighting(defaultHighlightStyle, {fallback: true}),
        bracketMatching(),
        closeBrackets(),
        autocompletion(),
        rectangularSelection(),
        crosshairCursor(),
        highlightActiveLine(),
        highlightSelectionMatches(),
        language.of(sqlExtension(currentState)),
        theme.of(editorTheme(currentState)),
        tabCompletion.of(tabCompletionExtension(currentState)),
        keymap.of([{
          key: "Ctrl-Enter",
          run(view) {
            post({type: "run", value: view.state.doc.toString()});
            return true;
          }
        }]),
        keymap.of([
          ...closeBracketsKeymap,
          ...defaultKeymap,
          ...searchKeymap,
          ...historyKeymap,
          ...foldKeymap,
          ...completionKeymap,
          ...lintKeymap
        ]),
        EditorView.updateListener.of(update => {
          if (update.docChanged && !applyingRemoteChange) {
            post({type: "change", value: update.state.doc.toString()});
          }
        })
      ]
    });

    window.dataViewerSqlEditor = {
      setState(nextState) {
        currentState = {
          value: nextState.value ?? "",
          appTheme: nextState.appTheme ?? "system",
          editorTheme: nextState.editorTheme ?? "followapp",
          fontFamily: nextState.fontFamily ?? "Cascadia Code",
          fontSize: nextState.fontSize ?? 13,
          acceptCompletionOnTab: nextState.acceptCompletionOnTab ?? true,
          tableName: nextState.tableName ?? null,
          columns: Array.isArray(nextState.columns) ? nextState.columns : []
        };

        const effects = [
          language.reconfigure(sqlExtension(currentState)),
          theme.reconfigure(editorTheme(currentState)),
          tabCompletion.reconfigure(tabCompletionExtension(currentState))
        ];

        const currentValue = view.state.doc.toString();
        if (currentValue !== currentState.value) {
          applyingRemoteChange = true;
          view.dispatch({
            changes: {from: 0, to: currentValue.length, insert: currentState.value},
            effects
          });
          applyingRemoteChange = false;
        } else {
          view.dispatch({effects});
        }
      },
      focus() {
        view.focus();
      }
    };

    window.matchMedia?.("(prefers-color-scheme: dark)")?.addEventListener("change", () => {
      if (currentState.appTheme === "system" || currentState.editorTheme === "followapp") {
        window.dataViewerSqlEditor.setState(currentState);
      }
    });

    post({type: "ready"});
  </script>
</body>
</html>
""";
}

public record SqlCodeEditorElement(
    string Value,
    string? TableName,
    IReadOnlyList<string> Columns,
    ThemeMode AppThemeMode,
    CodeEditorSettings Settings,
    int SyncVersion,
    Action<string> OnTextChanged,
    Action<string> OnRunRequested) : Element;

internal sealed class SqlCodeEditorRuntime(SqlCodeEditorElement element)
{
    public SqlCodeEditorElement Element { get; set; } = element;
}
