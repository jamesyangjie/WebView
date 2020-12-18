using System;
using System.Threading.Tasks;
using NUnit.Framework;
using WebViewControl;

namespace Tests.WebView {

    public class WebViewTestBase : TestBase<WebViewControl.WebView> {

        protected override void InitializeView() {
            if (TargetView != null) {
                TargetView.UnhandledAsyncException += OnUnhandledAsyncException;
            }
            base.InitializeView();
        }

        protected override async Task AfterInitializeView() {
            await base.AfterInitializeView();

            var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            TargetView.WebViewInitialized += () => {
                taskCompletionSource.SetResult(true);
            };

            await taskCompletionSource.Task;
            await Load("<html><script>;</script><body>Test page</body></html>");
        }

        protected Task Load(string html) {
            var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            
            void OnNavigated(string url, string frameName) {
                if (url != UrlHelper.AboutBlankUrl && !UrlHelper.IsChromeInternalUrl(url)) {
                    TargetView.Navigated -= OnNavigated;
                    taskCompletionSource.SetResult(true);
                }
            }

            void OnLoadFailed(string url, int errorCode, string frameName) {
                taskCompletionSource.SetException(new Exception("Error loading html: " + errorCode));
            }

            TargetView.LoadFailed += OnLoadFailed;
            TargetView.Navigated += OnNavigated;
            TargetView.LoadHtml(html);
            return taskCompletionSource.Task;
        }

        protected async Task WithUnhandledExceptionHandling(AsyncTestDelegate action, Func<Exception, bool> onException) {
            void OnUnhandledException(UnhandledAsyncExceptionEventArgs e) {
                e.Handled = onException(e.Exception);
            }

            var failOnAsyncExceptions = FailOnAsyncExceptions;
            FailOnAsyncExceptions = false;
            TargetView.UnhandledAsyncException += OnUnhandledException;

            try {
                await action();
            } finally {
                TargetView.UnhandledAsyncException -= OnUnhandledException;
                FailOnAsyncExceptions = failOnAsyncExceptions;
            }
        }

        protected override bool ShowDebugConsole() {
            if (TargetView.IsDisposing) {
                return false;
            }
            TargetView.ShowDeveloperTools();
            return true;
        }
    }
}
