using System.Text.RegularExpressions;
using Microsoft.Playwright;

public class ScreenshotService : IAsyncDisposable {
    private readonly Task<IPlaywright> _playwrightTask;
    private readonly Task<IBrowser> _browserTask;

    private static readonly TimeSpan NavigationTimeout = TimeSpan.FromSeconds( 20 );

    public ScreenshotService() {
        _playwrightTask = Microsoft.Playwright.Playwright.CreateAsync();

        _browserTask = _playwrightTask.ContinueWith( async t => {
            var playwright = await t;

            return await playwright.Chromium.LaunchAsync( new BrowserTypeLaunchOptions {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-dev-shm-usage"
                }
            } );
        } ).Unwrap();
    }

    public async Task<byte[]> CaptureAsync( string url ) {
        ValidateUrl( url );

        var browser = await _browserTask;

        await using var context = await browser.NewContextAsync( new BrowserNewContextOptions {
            ViewportSize = new ViewportSize {
                Width = 1920,
                Height = 1080
            }
        } );

        var page = await context.NewPageAsync();

        page.SetDefaultNavigationTimeout( ( float ) NavigationTimeout.TotalMilliseconds );

        try {
            await page.GotoAsync( url, new PageGotoOptions {
                WaitUntil = WaitUntilState.DOMContentLoaded
            } );

            try {
                await page.WaitForLoadStateAsync( LoadState.NetworkIdle, new() {
                    Timeout = 5000
                } );
            } catch {
                // Fine, just continue.
            }

            // small buffer for JS rendering
            await page.WaitForTimeoutAsync( 500 );

            return await page.ScreenshotAsync( new PageScreenshotOptions {
                FullPage = false,
                Type = ScreenshotType.Png
            } );
        } finally {
            await page.CloseAsync();
        }
    }

    private static void ValidateUrl( string url ) {
        if ( !Uri.TryCreate( url, UriKind.Absolute, out var uri ) )
            throw new ArgumentException( "Invalid URL" );

        if ( uri.Scheme != "http" && uri.Scheme != "https" )
            throw new ArgumentException( "Only HTTP/HTTPS allowed" );

        // Optional: block internal network (SSRF protection)
        if ( Regex.IsMatch( uri.Host, @"^(localhost|127\.|10\.|192\.168|::1)" ) )
            throw new ArgumentException( "Private network access blocked" );
    }

    public async ValueTask DisposeAsync() {
        if ( _browserTask.IsCompletedSuccessfully ) {
            var browser = await _browserTask;
            await browser.CloseAsync();
        }

        if ( _playwrightTask.IsCompletedSuccessfully ) {
            var playwright = await _playwrightTask;
            playwright.Dispose();
        }
    }
}