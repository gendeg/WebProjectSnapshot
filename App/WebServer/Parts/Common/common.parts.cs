namespace PartsCommon;

public static class CommonParts
{
    public const string HTMLDocStart = """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <link rel="stylesheet" href="/css/style.css">
    <script src="/scripts/main.js"></script>
    
""";

    public const string HeadClose = "</head>\n";

    public const string HTMLDocEnd = "</html>\n";
}