namespace neco_board_ce.Utils.Docs
{
    /// <summary>
    /// Self-contained HTML viewer for the generated XML documentation served at <c>/docs/full/raw</c>.
    /// No external dependencies: it fetches the raw XML in the browser, parses it and renders
    /// members grouped by type, with a live search box.
    /// </summary>
    public static class DocsXmlUi
    {
        public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>API XML Documentation</title>
<style>
  :root { --bg:#0f1115; --panel:#171a21; --border:#262b36; --fg:#d7dce5; --muted:#8b94a7; --accent:#5b9dff; --code:#1d222c; }
  * { box-sizing:border-box; }
  body { margin:0; font-family:system-ui,Segoe UI,Roboto,sans-serif; background:var(--bg); color:var(--fg); }
  header { position:sticky; top:0; background:var(--panel); border-bottom:1px solid var(--border); padding:16px 24px; display:flex; gap:16px; align-items:center; flex-wrap:wrap; }
  header h1 { font-size:18px; margin:0; }
  #q { flex:1; min-width:220px; padding:8px 12px; background:var(--bg); border:1px solid var(--border); border-radius:8px; color:var(--fg); font-size:14px; }
  #count { color:var(--muted); font-size:13px; }
  main { padding:24px; max-width:1000px; margin:0 auto; }
  section.type { background:var(--panel); border:1px solid var(--border); border-radius:12px; padding:18px 20px; margin-bottom:18px; }
  section.type > h2 { margin:0 0 4px; font-size:20px; }
  section.type > h2 .ns { display:block; font-size:12px; color:var(--muted); font-weight:400; margin-top:2px; word-break:break-all; }
  .member { border-top:1px solid var(--border); padding:14px 0 4px; }
  .member h3 { margin:0 0 6px; font-size:15px; font-family:ui-monospace,Consolas,monospace; }
  .summary { margin:4px 0; }
  .remarks { color:var(--muted); font-size:14px; margin:6px 0; }
  .returns { font-size:14px; margin:6px 0; }
  dl.params { margin:8px 0; display:grid; grid-template-columns:auto 1fr; gap:4px 12px; font-size:14px; }
  dl.params dt { font-family:ui-monospace,Consolas,monospace; color:var(--accent); }
  dl.params dd { margin:0; color:var(--muted); }
  code { background:var(--code); padding:1px 6px; border-radius:5px; font-family:ui-monospace,Consolas,monospace; font-size:.92em; }
  .badge { display:inline-block; font-size:10px; text-transform:uppercase; letter-spacing:.5px; background:var(--code); color:var(--muted); border:1px solid var(--border); border-radius:5px; padding:1px 6px; margin-right:8px; vertical-align:middle; }
  p { margin:6px 0; }
  ul { margin:6px 0; padding-left:20px; }
</style>
</head>
<body>
<header>
  <h1>XML Documentation</h1>
  <input id="q" placeholder="Search types and members…" autocomplete="off">
  <span id="count"></span>
</header>
<main id="content">Loading…</main>
<script>
const X = s => (s||'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');

function shortName(cref){
  let s = (cref||'').replace(/^[A-Za-z]:/,'').replace(/\(.*\)$/,'');
  const parts = s.split('.');
  return parts[parts.length-1] || s;
}

function render(node){
  let out = '';
  node.childNodes.forEach(n => {
    if (n.nodeType === 3) { out += X(n.nodeValue); }
    else if (n.nodeType === 1) {
      const tag = n.nodeName.toLowerCase();
      if (tag === 'see' || tag === 'seealso') {
        const cref = n.getAttribute('cref') || n.getAttribute('langword') || n.getAttribute('href');
        out += '<code>' + (cref ? X(shortName(cref)) : render(n)) + '</code>';
      } else if (tag === 'c') {
        out += '<code>' + render(n) + '</code>';
      } else if (tag === 'paramref' || tag === 'typeparamref') {
        out += '<code>' + X(n.getAttribute('name') || '') + '</code>';
      } else if (tag === 'para') {
        out += '<p>' + render(n) + '</p>';
      } else if (tag === 'list') {
        out += '<ul>' + render(n) + '</ul>';
      } else if (tag === 'item') {
        out += '<li>' + render(n) + '</li>';
      } else {
        out += render(n);
      }
    }
  });
  return out.replace(/\s+/g,' ').trim();
}

function field(el, tag){
  const e = el.querySelector(':scope > ' + tag);
  return e ? render(e) : '';
}

const KIND = { M:'method', P:'property', F:'field', E:'event' };

function renderType(fullName, info){
  const short = fullName.split('.').pop();
  let html = '<section class="type"><h2>' + X(short) + '<span class="ns">' + X(fullName) + '</span></h2>';
  if (info.doc){
    const s = field(info.doc,'summary'); if (s) html += '<div class="summary">' + s + '</div>';
    const r = field(info.doc,'remarks'); if (r) html += '<div class="remarks">' + r + '</div>';
  }
  info.members.sort((a,b)=>a.name.localeCompare(b.name)).forEach(mb => {
    html += '<div class="member"><h3><span class="badge">' + (KIND[mb.kind]||'') + '</span>' + X(mb.name) + '</h3>';
    const s = field(mb.el,'summary'); if (s) html += '<div class="summary">' + s + '</div>';
    const params = [...mb.el.querySelectorAll(':scope > param')];
    if (params.length){
      html += '<dl class="params">';
      params.forEach(p => { html += '<dt>' + X(p.getAttribute('name')||'') + '</dt><dd>' + render(p) + '</dd>'; });
      html += '</dl>';
    }
    const r = field(mb.el,'remarks'); if (r) html += '<div class="remarks">' + r + '</div>';
    const ret = field(mb.el,'returns'); if (ret) html += '<div class="returns"><b>Returns:</b> ' + ret + '</div>';
    html += '</div>';
  });
  return html + '</section>';
}

async function load(){
  const main = document.getElementById('content');
  let res;
  try { res = await fetch('/docs/full/raw'); } catch { main.textContent = 'Could not reach /docs/full/raw'; return; }
  if (!res.ok){ main.textContent = '/docs/full/raw returned ' + res.status; return; }

  const xml = new DOMParser().parseFromString(await res.text(), 'application/xml');
  if (xml.querySelector('parsererror')){ main.textContent = 'Failed to parse XML'; return; }

  const types = {};
  xml.querySelectorAll('members > member').forEach(m => {
    const name = m.getAttribute('name') || '';
    const kind = name[0];
    const path = name.slice(2).replace(/\(.*\)$/,'');
    let typeName, memberName = null;
    if (kind === 'T') { typeName = path; }
    else { const i = path.lastIndexOf('.'); typeName = path.slice(0,i); memberName = path.slice(i+1); }
    (types[typeName] = types[typeName] || { doc:null, members:[] });
    if (kind === 'T') types[typeName].doc = m;
    else types[typeName].members.push({ kind, name: memberName, el: m });
  });

  const names = Object.keys(types).sort();
  document.getElementById('count').textContent = names.length + ' types';
  main.innerHTML = names.map(t => renderType(t, types[t])).join('') || 'No documented members found.';
}

document.getElementById('q').addEventListener('input', e => {
  const term = e.target.value.toLowerCase();
  document.querySelectorAll('section.type').forEach(sec => {
    const typeHit = sec.querySelector('h2').textContent.toLowerCase().includes(term);
    let any = false;
    sec.querySelectorAll('.member').forEach(mm => {
      const hit = !term || typeHit || mm.textContent.toLowerCase().includes(term);
      mm.style.display = hit ? '' : 'none';
      if (hit) any = true;
    });
    sec.style.display = (!term || typeHit || any) ? '' : 'none';
  });
});

load();
</script>
</body>
</html>
""";
    }
}
