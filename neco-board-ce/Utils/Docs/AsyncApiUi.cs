namespace neco_board_ce.Utils.Docs
{
    /// <summary>
    /// Minimal AsyncAPI viewer page. Loads a modern <c>@asyncapi/react-component</c> from a CDN
    /// and renders the Saunter-generated document at <c>/asyncapi/asyncapi.json</c>.
    /// </summary>
    /// <remarks>
    /// Saunter 0.13 ships an ancient bundled UI (asyncapi-react 1.0.1) that cannot render the
    /// AsyncAPI 2.4.0 document it generates, so we serve our own page instead. Requires network
    /// access to the CDN at view time.
    /// </remarks>
    public static class AsyncApiUi
    {
        public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Board CE sockets</title>
<link rel="stylesheet" href="https://unpkg.com/@asyncapi/react-component@2.6.4/styles/default.min.css">
<style>
  body { margin: 0; font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif; }
</style>
</head>
<body>
<div id="asyncapi"></div>
<script src="https://unpkg.com/@asyncapi/react-component@2.6.4/browser/standalone/index.js"></script>
<script>
  AsyncApiStandalone.render({
    schema: { url: '/asyncapi/asyncapi.json', options: { method: 'GET' } },
    config: { show: { sidebar: true, errors: true } },
  }, document.getElementById('asyncapi'));
</script>
</body>
</html>
""";
    }
}
