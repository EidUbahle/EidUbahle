<%@ Page Language="C#" AutoEventWireup="true" %>
<!DOCTYPE html>
<html>
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Error – EidUbahle ERP</title>
  <link rel="stylesheet" href="/Styles/main.css" />
  <link rel="stylesheet" href="/Styles/themes.css" />
</head>
<body>
  <div style="min-height:100vh;display:flex;align-items:center;justify-content:center;padding:1.5rem;background:var(--bg-base);">
    <div style="text-align:center;max-width:480px;">
      <div style="font-size:4rem;margin-bottom:1rem;">⚠️</div>
      <h1 style="font-size:1.5rem;margin-bottom:.75rem;">Something went wrong</h1>
      <p style="color:var(--text-muted);margin-bottom:1.5rem;">An unexpected error occurred. Please try again or contact your administrator.</p>
      <a href="/" style="display:inline-flex;align-items:center;gap:.5rem;background:var(--accent);color:var(--accent-text);padding:.75rem 2rem;border-radius:var(--radius-md);font-weight:600;text-decoration:none;">
        ← Return Home
      </a>
    </div>
  </div>
  <script src="/Scripts/modules/theme.js"></script>
  <script>ThemeEngine.init(null);</script>
</body>
</html>
