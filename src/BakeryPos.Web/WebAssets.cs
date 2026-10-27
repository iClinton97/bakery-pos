namespace BakeryPos.Web;

/// <summary>The page, styles and script, kept as text so there are no files to ship.</summary>
internal static class WebAssets
{
    public static string StockPage(int refreshMs) =>
        StockHtml.Replace("{{refreshMs}}", refreshMs.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public static string LoginPage(string? message) =>
        LoginHtml.Replace("{{message}}", System.Net.WebUtility.HtmlEncode(message ?? ""));

    private const string LoginHtml = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Bakery stock</title>
<link rel="stylesheet" href="/app.css">
</head>
<body class="login">
<main class="card">
<h1>Bakery stock</h1>
<p>Enter the PIN to see live stock.</p>
<form method="post" action="/login">
<label for="pin">PIN</label>
<input id="pin" name="pin" type="password" inputmode="numeric" autocomplete="off" autofocus required>
<button type="submit">Open</button>
</form>
<p class="error" role="alert">{{message}}</p>
</main>
</body>
</html>
""";

    private const string StockHtml = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Live stock</title>
<link rel="stylesheet" href="/app.css">
</head>
<body data-refresh-ms="{{refreshMs}}">
<header>
<div><h1>Live stock</h1><div id="headline" class="headline"></div></div>
<div class="right">
<span id="live" class="live"><span class="dot"></span><span id="liveText">Loading</span></span>
<form method="post" action="/logout"><button type="submit" class="plain">Lock</button></form>
</div>
</header>
<div id="stale" class="stale" hidden>Not updating. Check the wifi and the till.</div>
<section id="totals" class="totals" aria-label="Totals"></section>
<div class="layout">
<div class="tables">
<section><h2>Pie booth</h2><p class="sub">What is out and ready to sell. The cashier sells from this count.</p><div id="pieBooth"></div></section>
<section><h2>Fridge</h2><p class="sub">Stored in the fridge, not yet out in the booth.</p><div id="fridge"></div></section>
<section><h2>Hot food</h2><div id="hotFood"></div></section>
<section><h2>Cakes</h2><div id="cakes"></div></section>
<section><h2>Savoury</h2><div id="savoury"></div></section>
</div>
<aside><h2>Refill list</h2><div id="refills"></div></aside>
</div>
<script src="/app.js"></script>
</body>
</html>
""";

    public const string Css = """
* { box-sizing: border-box; }
body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", sans-serif; background: #F1F3F6; color: #1B2430; padding: 16px; }
h1 { margin: 0; font-size: 28px; }
h2 { margin: 0; font-size: 20px; }
.sub { margin: 4px 0 0; color: #4A5563; font-size: 14px; }
header { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 12px; }
.right { display: flex; align-items: center; gap: 12px; }
.headline { color: #B4440B; font-weight: 700; margin-top: 2px; }
.live { display: flex; align-items: center; gap: 8px; color: #4A5563; font-size: 15px; }
.dot { width: 10px; height: 10px; border-radius: 50%; background: #1E6B3A; }
.live.old .dot { background: #A32020; }
.stale { background: #FBE9E9; color: #A32020; font-weight: 700; padding: 12px 16px; border-radius: 12px; margin-bottom: 12px; }
.totals { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; margin-bottom: 12px; }
.total { background: #fff; border: 1px solid #D5DAE0; border-radius: 12px; padding: 14px; }
.total span { display: block; color: #4A5563; font-size: 14px; }
.total strong { font-size: 30px; }
.layout { display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-start; }
.tables { flex: 999 1 560px; min-width: 0; display: flex; flex-direction: column; gap: 16px; }
.tables section { background: #fff; border: 1px solid #D5DAE0; border-radius: 16px; padding: 16px; overflow-x: auto; }
aside { flex: 1 1 300px; max-width: 420px; background: #1B2430; color: #fff; border-radius: 16px; padding: 20px; display: flex; flex-direction: column; gap: 14px; }
.row { display: grid; grid-template-columns: minmax(120px, 1.4fr) minmax(80px, 2fr) 56px 56px 90px; gap: 12px; align-items: center; padding: 10px 0; border-bottom: 1px solid #E3E7EC; font-size: 16px; }
.row.fridge { grid-template-columns: minmax(120px, 1.4fr) 80px 80px 80px; }
.row.head { font-size: 13px; font-weight: 700; color: #4A5563; }
.row:last-child { border-bottom: 0; }
.name { font-weight: 600; }
.bar { height: 12px; border-radius: 6px; background: #E3E7EC; }
.bar > div { height: 12px; border-radius: 6px; background: #1F4E8C; }
.bar.low > div { background: #B4440B; }
.bar.soldOut > div { background: #A32020; }
.badge { justify-self: start; padding: 3px 10px; border-radius: 999px; font-size: 13px; font-weight: 700; }
.badge.low { background: #FBEDE4; color: #B4440B; }
.badge.soldOut { background: #FBE9E9; color: #A32020; }
.ok { color: #4A5563; font-weight: 600; }
.empty { color: #A32020; font-weight: 700; }
.refill { display: flex; flex-direction: column; gap: 4px; padding-bottom: 12px; border-bottom: 1px solid #4A5563; }
.refill:last-child { border-bottom: 0; }
.refill .top { display: flex; justify-content: space-between; gap: 8px; }
.refill .top b { font-size: 17px; }
.refill .top span { color: #C9D1DB; font-size: 14px; }
.refill .empty { color: #FFD2B8; }
.nothing { color: #C9D1DB; }
button { font: inherit; cursor: pointer; }
.plain { min-height: 40px; padding: 0 16px; border-radius: 20px; border: 1px solid #D5DAE0; background: #fff; color: #1B2430; }
.login { display: flex; justify-content: center; padding-top: 12vh; }
.card { width: 100%; max-width: 360px; background: #fff; border: 1px solid #D5DAE0; border-radius: 16px; padding: 24px; display: flex; flex-direction: column; gap: 12px; height: fit-content; }
.card form { display: flex; flex-direction: column; gap: 8px; }
.card input { font-size: 28px; letter-spacing: 6px; padding: 10px 12px; border: 1px solid #D5DAE0; border-radius: 10px; }
.card button { min-height: 52px; border: 0; border-radius: 12px; background: #1F4E8C; color: #fff; font-size: 18px; font-weight: 700; }
.error { color: #A32020; font-weight: 700; min-height: 1.2em; margin: 0; }
""";

    public const string Script = """
(function () {
  "use strict";

  var refreshMs = Number(document.body.dataset.refreshMs) || 5000;
  var staleAfterMs = refreshMs * 3;
  var lastGood = Date.now();

  function el(tag, className, text) {
    var node = document.createElement(tag);
    if (className) { node.className = className; }
    if (text !== undefined) { node.textContent = text; }
    return node;
  }

  function badge(status) {
    if (status === "soldOut") { return el("span", "badge soldOut", "Sold out"); }
    if (status === "low") { return el("span", "badge low", "Low"); }
    return el("span", "ok", "OK");
  }

  function boothRow(row) {
    var pct = row.par > 0 ? Math.min(100, Math.round(row.booth / row.par * 100)) : 0;
    var line = el("div", "row");
    line.appendChild(el("span", "name", row.name));
    var bar = el("div", "bar " + row.status);
    var fill = el("div");
    fill.style.width = pct + "%";
    bar.appendChild(fill);
    line.appendChild(bar);
    line.appendChild(el("b", "", String(row.booth)));
    line.appendChild(el("span", "", "/ " + row.par));
    line.appendChild(badge(row.status));
    return line;
  }

  function boothTable(target, rows, firstHeading) {
    target.replaceChildren();
    var head = el("div", "row head");
    [firstHeading, "Level", "Booth", "Par", "Status"].forEach(function (t) { head.appendChild(el("span", "", t)); });
    target.appendChild(head);
    rows.forEach(function (r) { target.appendChild(boothRow(r)); });
  }

  function fridgeTable(target, rows) {
    target.replaceChildren();
    var head = el("div", "row head fridge");
    ["Pie", "Fridge", "Booth", "Total"].forEach(function (t) { head.appendChild(el("span", "", t)); });
    target.appendChild(head);
    rows.slice().sort(function (a, b) { return a.fridge - b.fridge; }).forEach(function (r) {
      var line = el("div", "row fridge");
      line.appendChild(el("span", "name", r.name));
      line.appendChild(el("b", r.fridge === 0 ? "empty" : "", String(r.fridge)));
      line.appendChild(el("span", "", String(r.booth)));
      line.appendChild(el("span", "", String(r.fridge + r.booth)));
      target.appendChild(line);
    });
  }

  function total(label, value) {
    var box = el("div", "total");
    box.appendChild(el("span", "", label));
    box.appendChild(el("strong", "", String(value)));
    return box;
  }

  function refills(target, items) {
    target.replaceChildren();
    if (items.length === 0) {
      target.appendChild(el("div", "nothing", "Nothing needs refilling."));
      return;
    }
    items.forEach(function (r) {
      var box = el("div", "refill");
      var top = el("div", "top");
      top.appendChild(el("b", "", r.name));
      top.appendChild(el("span", "", r.status === "soldOut" ? "Sold out" : r.booth + " left"));
      box.appendChild(top);
      if (r.category === "pies") {
        if (r.fridge === 0) { box.appendChild(el("div", "empty", "Fridge empty. Make more.")); }
        else if (r.fridgeShortfall > 0) { box.appendChild(el("div", "", "Bring out " + r.bringOut + " from fridge (all of it)")); }
        else { box.appendChild(el("div", "", "Bring out " + r.bringOut + " from fridge")); }
      } else {
        box.appendChild(el("div", "", "Bring out " + (r.bringOut + r.fridgeShortfall)));
      }
      target.appendChild(box);
    });
  }

  function render(report) {
    var by = function (cat) { return report.rows.filter(function (r) { return r.category === cat; }); };
    var pies = by("pies");
    var boothTotal = pies.reduce(function (s, r) { return s + r.booth; }, 0);
    var fridgeTotal = pies.reduce(function (s, r) { return s + r.fridge; }, 0);

    document.getElementById("headline").textContent =
      report.refills.length === 1 ? "1 item needs refilling" : report.refills.length + " items need refilling";

    var totals = document.getElementById("totals");
    totals.replaceChildren(
      total("Pies in the booth", boothTotal),
      total("Pies in the fridge", fridgeTotal),
      total("Total pies on hand", boothTotal + fridgeTotal));

    boothTable(document.getElementById("pieBooth"), pies, "Pie");
    fridgeTable(document.getElementById("fridge"), pies);
    boothTable(document.getElementById("hotFood"), by("hotFood"), "Item");
    boothTable(document.getElementById("cakes"), by("cakes"), "Item");
    boothTable(document.getElementById("savoury"), by("savoury"), "Item");
    refills(document.getElementById("refills"), report.refills);
  }

  function showAge() {
    var old = Date.now() - lastGood > staleAfterMs;
    document.getElementById("stale").hidden = !old;
    document.getElementById("live").className = old ? "live old" : "live";
    document.getElementById("liveText").textContent = old
      ? "Not updating"
      : "Live · updated " + new Date(lastGood).toLocaleTimeString();
  }

  function load() {
    fetch("/api/stock", { credentials: "same-origin", cache: "no-store" })
      .then(function (response) {
        if (response.status === 401) { window.location.href = "/login"; return null; }
        if (!response.ok) { throw new Error("status " + response.status); }
        return response.json();
      })
      .then(function (report) {
        if (report) { render(report); lastGood = Date.now(); }
        showAge();
      })
      .catch(function () { showAge(); });
  }

  load();
  setInterval(load, refreshMs);
  setInterval(showAge, 1000);
})();
""";
}
