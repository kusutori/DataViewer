namespace DataViewer.State;

public static class AppReducer
{
    public static AppState Reduce(AppState state, AppAction action) =>
        action switch
        {
            OpenFileRequested => state with
            {
                IsBusy = true,
                ErrorMessage = null,
            },
            FileLoaded loaded => state with
            {
                DataSet = loaded.DataSet,
                SqlText = loaded.SqlText,
                Result = loaded.Result,
                IsBusy = false,
                ErrorMessage = null,
            },
            LoadFailed failed => state with
            {
                IsBusy = false,
                ErrorMessage = failed.Message,
            },
            SqlChanged changed => state with
            {
                SqlText = changed.Value,
            },
            RunQueryRequested => state with
            {
                IsBusy = true,
                ErrorMessage = null,
            },
            QuerySucceeded succeeded => state with
            {
                Result = succeeded.Result,
                IsBusy = false,
                ErrorMessage = null,
            },
            QueryFailed failed => state with
            {
                IsBusy = false,
                ErrorMessage = failed.Message,
            },
            ResetQuery reset => state with
            {
                SqlText = reset.SqlText,
                Result = reset.Result,
                IsBusy = false,
                ErrorMessage = null,
            },
            DismissToast => state with
            {
                ErrorMessage = null,
            },
            ThemeChanged changed => state with
            {
                ThemeMode = changed.Value,
            },
            CompletionItemsChanged changed => state with
            {
                CompletionItems = changed.Items,
            },
            CompletionAccepted accepted => state with
            {
                SqlText = accepted.SqlText,
                CompletionItems = [],
            },
            _ => state,
        };
}
