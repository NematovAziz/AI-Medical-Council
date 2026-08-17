/* =========================================================
   AI MEDICAL COUNCIL — client runtime
   counters · risk rings · trend charts · council stream · lab drag & drop
   ========================================================= */
(function () {
  'use strict';

  var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ---------- animated counters ---------- */
  function countUp(el) {
    var target = parseFloat(el.getAttribute('data-count') || '0');
    if (reduce || !target) { el.textContent = fmt(target); return; }
    var start = performance.now(), dur = 1100;
    function step(now) {
      var p = Math.min(1, (now - start) / dur);
      el.textContent = fmt(Math.round(target * (1 - Math.pow(1 - p, 3))));
      if (p < 1) requestAnimationFrame(step);
    }
    requestAnimationFrame(step);
  }
  function fmt(n) { return Number(n).toLocaleString('ru-RU'); }

  /* ---------- scroll reveal ---------- */
  function initReveal() {
    var items = document.querySelectorAll('.reveal');
    if (!items.length) return;
    if (reduce || !('IntersectionObserver' in window)) {
      items.forEach(function (el) { el.classList.add('in'); });
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (e) {
        if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); }
      });
    }, { threshold: 0.08 });
    items.forEach(function (el) { io.observe(el); });
  }

  /* ---------- risk ring ---------- */
  function paintRing(wrap) {
    var val = parseInt(wrap.getAttribute('data-score') || '0', 10);
    var arc = wrap.querySelector('.ring-val');
    if (!arc) return;
    var c = 2 * Math.PI * parseFloat(arc.getAttribute('r'));
    arc.setAttribute('stroke-dasharray', c.toFixed(1));
    arc.setAttribute('stroke-dashoffset', c.toFixed(1));
    arc.setAttribute('stroke', val >= 85 ? '#DC2544' : val >= 60 ? '#C97A00' : '#1467D6');
    requestAnimationFrame(function () {
      arc.setAttribute('stroke-dashoffset', (c * (1 - Math.min(100, val) / 100)).toFixed(1));
    });
  }

  /* ---------- reference gauges on lab tables ---------- */
  function paintGauge(el) {
    var v = parseFloat(el.getAttribute('data-value'));
    var lo = parseFloat(el.getAttribute('data-low'));
    var hi = parseFloat(el.getAttribute('data-high'));
    if (isNaN(v) || isNaN(lo) || isNaN(hi) || hi <= lo) return;

    var span = hi - lo;
    var min = lo - span * 0.8, max = hi + span * 0.8;
    function pct(x) { return Math.max(0, Math.min(100, ((x - min) / (max - min)) * 100)); }

    var band = document.createElement('span');
    band.className = 'band';
    band.style.left = pct(lo) + '%';
    band.style.width = (pct(hi) - pct(lo)) + '%';

    var pin = document.createElement('span');
    pin.className = 'pin' + (v < lo || v > hi ? ' out' : '');
    pin.style.left = '0%';

    el.appendChild(band);
    el.appendChild(pin);
    setTimeout(function () { pin.style.left = pct(v) + '%'; }, 60);
  }

  /* ---------- trend chart ---------- */
  function drawTrend(svg) {
    var raw = svg.getAttribute('data-series');
    if (!raw) return;
    var series;
    try { series = JSON.parse(raw); } catch (e) { return; }
    if (!series.length) return;

    var W = 680, H = 190, padL = 44, padR = 14, padT = 16, padB = 24;
    svg.setAttribute('viewBox', '0 0 ' + W + ' ' + H);

    var all = [];
    series.forEach(function (s) { s.points.forEach(function (p) { all.push(p); }); });
    if (!all.length) return;

    var min = Math.min.apply(null, all), max = Math.max.apply(null, all);
    if (max === min) max = min + 1;
    var pad = (max - min) * 0.15;
    min -= pad; max += pad;

    var n = series[0].points.length;
    function x(i) { return n < 2 ? padL : padL + (i * (W - padL - padR)) / (n - 1); }
    function y(v) { return padT + (H - padT - padB) * (1 - (v - min) / (max - min)); }

    var out = '';
    for (var g = 0; g <= 3; g++) {
      var gy = padT + (g * (H - padT - padB)) / 3;
      out += '<line class="gl" x1="' + padL + '" y1="' + gy.toFixed(1) + '" x2="' + (W - padR) + '" y2="' + gy.toFixed(1) + '"/>';
      out += '<text x="6" y="' + (gy + 4).toFixed(1) + '" fill="#7D96B4" font-size="12" font-family="Times New Roman, serif">'
           + (max - (g * (max - min)) / 3).toFixed(0) + '</text>';
    }
    series.forEach(function (s, si) {
      var d = '';
      s.points.forEach(function (p, i) { d += (i === 0 ? 'M' : 'L') + x(i).toFixed(1) + ',' + y(p).toFixed(1); });
      out += '<path class="ln" d="' + d + '" stroke="' + s.color + '" style="animation-delay:' + (si * 0.25) + 's"/>';
      s.points.forEach(function (p, i) {
        out += '<circle cx="' + x(i).toFixed(1) + '" cy="' + y(p).toFixed(1) + '" r="4" fill="#fff" stroke="' + s.color + '" stroke-width="2.5"/>';
      });
    });
    svg.innerHTML = out;
  }

  /* ---------- live council stream ---------- */
  function startCouncil(root) {
    var url = root.getAttribute('data-stream');
    if (!url || typeof EventSource === 'undefined') return;

    var log = document.getElementById('council-log');
    var ring = root.querySelector('.core-wrap');
    var score = root.querySelector('.core-node b');
    var phase = document.getElementById('council-phase');
    var done = 0;

    function line(html, cls) {
      if (!log) return;
      var d = document.createElement('div');
      d.className = 'log-line ' + (cls || '');
      d.innerHTML = '<span class="faint">' + new Date().toLocaleTimeString('ru-RU') + '</span> ' + html;
      log.appendChild(d);
      log.scrollTop = log.scrollHeight;
    }

    Array.prototype.forEach.call(root.querySelectorAll('.ag'), function (el, i) {
      setTimeout(function () { el.classList.add('live'); }, 130 * i);
    });

    var es = new EventSource(url);

    es.addEventListener('phase', function (ev) {
      var d = JSON.parse(ev.data);
      if (phase) phase.textContent = d.label;
      line('<b style="color:#1467D6">' + d.label + '</b>');
    });

    es.addEventListener('agent', function (ev) {
      var d = JSON.parse(ev.data);
      done++;
      var el = root.querySelector('[data-agent="' + d.agent.replace(/"/g, '\\"') + '"]');
      if (el) {
        el.classList.remove('live');
        el.classList.add('done');
        if (d.available === false) el.classList.add('sev-off');
        else if (d.severity === 'Critical') el.classList.add('sev-c');
        else if (d.severity === 'Warning') el.classList.add('sev-w');
        var c = el.querySelector('.n em'); if (c) c.textContent = d.available === false ? '—' : d.confidence + '%';
        var b = el.querySelector('.bar i'); if (b) b.style.width = (d.available === false ? 0 : d.confidence) + '%';
        var s = el.querySelector('.s'); if (s) s.textContent = d.source;
        var t = el.querySelector('.txt'); if (t) t.textContent = d.finding;
      }
      line(d.available === false
        ? '<b>' + d.agent + '</b> — <span style="color:#DC2544">ulanmadi</span> · ' + d.source
        : '<b>' + d.agent + '</b> — ' + d.severity + ' · ' + d.confidence + '%');
    });

    es.addEventListener('done', function (ev) {
      var d = JSON.parse(ev.data);
      if (ring) { ring.setAttribute('data-score', d.riskScore); paintRing(ring); }
      if (score) {
        score.textContent = d.riskScore;
        score.className = d.riskScore >= 85 ? 'c' : d.riskScore >= 60 ? 'w' : '';
      }
      if (phase) phase.textContent = 'YAKUNLANDI · ' + d.riskLevel.toUpperCase();
      line('<b style="color:#0C9A63">Konsilium yakunlandi · Risk ' + d.riskScore + '/100</b>');
      es.close();
      setTimeout(function () { window.location.href = d.resultUrl; }, 1800);
    });

    es.addEventListener('failed', function (ev) {
      if (phase) phase.textContent = 'XATOLIK';
      line('<b style="color:#DC2544">' + JSON.parse(ev.data).message + '</b>');
      es.close();
    });

    es.onerror = function () {
      if (done === 0 && phase) phase.textContent = 'ULANISH UZILDI';
      es.close();
    };
  }

  /* ---------- lab drag & drop ---------- */
  function initDropzone(zone) {
    var input = zone.querySelector('input[type=file]');
    var bar = zone.querySelector('.dz-progress');
    var fill = zone.querySelector('.dz-progress i');
    var stage = zone.querySelector('.dz-stage');
    var url = zone.getAttribute('data-url');
    var patientId = zone.getAttribute('data-patient');

    function say(text, cls) {
      if (!stage) return;
      stage.className = 'dz-stage ' + (cls || '');
      stage.textContent = text;
    }

    ['dragenter', 'dragover'].forEach(function (e) {
      zone.addEventListener(e, function (ev) { ev.preventDefault(); zone.classList.add('over'); });
    });
    ['dragleave', 'drop'].forEach(function (e) {
      zone.addEventListener(e, function (ev) { ev.preventDefault(); zone.classList.remove('over'); });
    });

    zone.addEventListener('drop', function (ev) {
      if (ev.dataTransfer && ev.dataTransfer.files.length) send(ev.dataTransfer.files[0]);
    });
    zone.addEventListener('click', function () { if (input) input.click(); });
    if (input) input.addEventListener('change', function () { if (input.files.length) send(input.files[0]); });

    function send(file) {
      var data = new FormData();
      data.append('file', file);
      data.append('patientId', patientId);

      zone.classList.add('busy');
      if (bar) bar.classList.add('on');
      if (fill) fill.style.width = '0%';
      say('Fayl yuklanmoqda: ' + file.name);

      var xhr = new XMLHttpRequest();
      xhr.open('POST', url, true);

      xhr.upload.onprogress = function (e) {
        if (!e.lengthComputable || !fill) return;
        var pct = Math.round((e.loaded / e.total) * 70);
        fill.style.width = pct + '%';
      };

      xhr.onload = function () {
        if (fill) fill.style.width = '88%';
        say('AI ko\u2019rsatkichlarni ajratmoqda\u2026');

        var res;
        try { res = JSON.parse(xhr.responseText); }
        catch (e) { finishError('Server javobini o\u2019qib bo\u2019lmadi.'); return; }

        if (!res.ok) { finishError(res.message || 'Xatolik.'); return; }

        if (fill) fill.style.width = '100%';
        say(res.extracted + ' ta ko\u2019rsatkich bazaga yozildi · ' + res.source, 'ok');

        setTimeout(function () {
          window.location.href = res.councilUrl || res.documentUrl;
        }, 1200);
      };

      xhr.onerror = function () { finishError('Tarmoq xatoligi.'); };

      function finishError(msg) {
        zone.classList.remove('busy');
        if (fill) fill.style.width = '0%';
        if (bar) bar.classList.remove('on');
        say(msg, 'err');
      }

      xhr.send(data);
    }
  }

  /* ---------- boot ---------- */
  document.addEventListener('DOMContentLoaded', function () {
    Array.prototype.forEach.call(document.querySelectorAll('[data-count]'), countUp);
    Array.prototype.forEach.call(document.querySelectorAll('.core-wrap[data-score]'), paintRing);
    Array.prototype.forEach.call(document.querySelectorAll('.trend[data-series]'), drawTrend);
    Array.prototype.forEach.call(document.querySelectorAll('.gauge[data-value]'), paintGauge);
    Array.prototype.forEach.call(document.querySelectorAll('.drop[data-url]'), initDropzone);

    Array.prototype.forEach.call(document.querySelectorAll('.tli'), function (el, i) {
      el.style.animationDelay = (i * 0.07) + 's';
    });

    initReveal();

    var live = document.getElementById('council-live');
    if (live) startCouncil(live);
  });
})();

/* =========================================================
   V6 — settings page: provider presets, connection test
   ========================================================= */
(function () {
  'use strict';

  function initSettings() {
    var form = document.getElementById('agent-form');
    if (!form) return;

    var cards = Array.prototype.slice.call(form.querySelectorAll('.agent-card'));
    var active = cards[0] || null;

    cards.forEach(function (card) {
      card.addEventListener('focusin', function () { active = card; });
      card.addEventListener('click', function () { active = card; });
    });

    // preset chips fill the focused card
    Array.prototype.forEach.call(document.querySelectorAll('.chip'), function (chip) {
      chip.addEventListener('click', function () {
        if (!active) return;
        var p = active.querySelector('.j-provider');
        var e = active.querySelector('.j-endpoint');
        var m = active.querySelector('.j-model');
        if (p) p.value = chip.getAttribute('data-name');
        if (e) e.value = chip.getAttribute('data-endpoint');
        if (m && !m.value) m.value = chip.getAttribute('data-model');

        Array.prototype.forEach.call(document.querySelectorAll('.chip'), function (c) { c.classList.remove('picked'); });
        chip.classList.add('picked');

        active.classList.remove('flash');
        void active.offsetWidth;
        active.classList.add('flash');
      });
    });

    // connection test
    Array.prototype.forEach.call(form.querySelectorAll('.j-test'), function (btn) {
      btn.addEventListener('click', function () {
        var card = btn.closest('.agent-card');
        var out = card.querySelector('.test-out');
        out.className = 'test-out wait';
        out.textContent = '⋯';

        var body = new FormData();
        body.append('key', btn.getAttribute('data-key'));

        fetch(btn.getAttribute('data-url'), { method: 'POST', body: body })
          .then(function (r) { return r.json(); })
          .then(function (d) {
            out.className = 'test-out ' + (d.ok ? 'ok' : 'err');
            out.textContent = (d.ok ? '✓ ' : '✕ ') + d.message;
          })
          .catch(function () {
            out.className = 'test-out err';
            out.textContent = '✕';
          });
      });
    });
  }

  document.addEventListener('DOMContentLoaded', initSettings);
})();
