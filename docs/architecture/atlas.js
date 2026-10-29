/*
 * atlas.js: the interactive part of an architecture page.
 * Copied unchanged from the architecture-views skill; pages never carry their own script.
 * A classic script on purpose: ES modules do not load from file://.
 *
 * Each <figure class="atlas-figure"> holding an <svg> gets the controls its markup asks for:
 *   ul.atlas-layers > li[data-layer]      a toggle per layer; li[data-off] starts hidden
 *   [data-layer="a b"] in the svg         shown only while every layer it names is on
 *   [data-node="id"]                      a box you can focus; [data-title] names it
 *   [data-edge="from to"]                 a link; hidden when either end is hidden
 *   .atlas-cards > [data-card="id"]       the card shown when that node is focused
 *   [data-step="n"] + ol.atlas-steps      a stepper; the nth li tells step n
 *   [data-since="YYYY-MM-DD"], [data-until="YYYY-MM-DD"]   a timeline slider
 */
(function () {
  "use strict";

  var STORE = "atlas:" + location.pathname;

  function load() {
    try { return JSON.parse(localStorage.getItem(STORE)) || {}; } catch (e) { return {}; }
  }

  function save(state) {
    try { localStorage.setItem(STORE, JSON.stringify(state)); } catch (e) { /* storage blocked: toggles still work */ }
  }

  function words(value) {
    return (value || "").split(/\s+/).filter(Boolean);
  }

  function each(list, fn) {
    Array.prototype.forEach.call(list, fn);
  }

  function make(tag, className, text) {
    var node = document.createElement(tag);
    if (className) node.className = className;
    if (text) node.textContent = text;
    return node;
  }

  function button(label, onClick) {
    var b = make("button", "atlas-button", label);
    b.type = "button";
    b.addEventListener("click", onClick);
    return b;
  }

  function nodeTitle(node) {
    if (node.getAttribute("data-title")) return node.getAttribute("data-title");
    var text = node.querySelector("text");
    return text ? text.textContent.trim() : node.getAttribute("data-node");
  }

  function setup(figure, index, saved) {
    var svg = figure.querySelector("svg");
    if (!svg) return;

    var holder = svg;
    while (holder.parentNode !== figure) holder = holder.parentNode;

    var id = figure.id || "figure-" + index;
    var memory = saved[id] || (saved[id] = {});
    var off = {};
    var at = null;

    var controls = make("div", "atlas-controls");
    figure.insertBefore(controls, holder);
    figure.classList.add("atlas-live");

    function visible(element) {
      var layers = words(element.getAttribute("data-layer"));
      for (var i = 0; i < layers.length; i++) if (off[layers[i]]) return false;
      var date = at || "9999-12-31";
      var since = element.getAttribute("data-since");
      var until = element.getAttribute("data-until");
      if (since && since > date) return false;
      if (until && until <= date) return false;
      return true;
    }

    var focus = { refresh: function () {} };

    function refresh() {
      each(svg.querySelectorAll("[data-layer],[data-since],[data-until]"), function (element) {
        element.classList.toggle("atlas-off", !visible(element));
      });
      var gone = {};
      each(svg.querySelectorAll("[data-node]"), function (node) {
        if (node.classList.contains("atlas-off") || node.closest(".atlas-off")) gone[node.getAttribute("data-node")] = true;
      });
      each(svg.querySelectorAll("[data-edge]"), function (edge) {
        var cut = words(edge.getAttribute("data-edge")).some(function (end) { return gone[end]; });
        edge.classList.toggle("atlas-cut", cut);
      });
      focus.refresh(gone);
    }

    layers(figure, controls, off, memory, saved, refresh);
    timeline(svg, controls, function (date) { at = date; refresh(); });
    focus = focusing(figure, svg, holder);
    stepping(figure, svg, controls);
    refresh();

    if (!controls.childNodes.length) controls.remove();
  }

  function layers(figure, controls, off, memory, saved, refresh) {
    var list = figure.querySelector("ul.atlas-layers");
    if (!list) return;
    var group = make("div", "atlas-group");
    group.setAttribute("role", "group");
    group.setAttribute("aria-label", "Layers");
    memory.layers = memory.layers || {};

    each(list.querySelectorAll("li[data-layer]"), function (item) {
      var key = item.getAttribute("data-layer");
      var on = key in memory.layers ? memory.layers[key] : !item.hasAttribute("data-off");
      off[key] = !on;

      var label = make("label", "atlas-toggle");
      var input = make("input");
      input.type = "checkbox";
      input.checked = on;
      input.addEventListener("change", function () {
        off[key] = !input.checked;
        memory.layers[key] = input.checked;
        save(saved);
        refresh();
      });
      label.appendChild(input);
      while (item.firstChild) label.appendChild(item.firstChild);
      group.appendChild(label);
    });

    list.hidden = true;
    controls.appendChild(group);
  }

  function timeline(svg, controls, onChange) {
    var dates = {};
    each(svg.querySelectorAll("[data-since],[data-until]"), function (element) {
      ["data-since", "data-until"].forEach(function (name) {
        var value = element.getAttribute(name);
        if (value) dates[value] = true;
      });
    });
    var sorted = Object.keys(dates).sort();
    if (!sorted.length) return;

    // A stop before the first change, one per change date, and the last one is today.
    var stops = [{ value: "0000-00-00", label: "Before " + sorted[0] }];
    sorted.forEach(function (date, i) {
      stops.push({ value: i === sorted.length - 1 ? null : date, label: i === sorted.length - 1 ? "Now (since " + date + ")" : date });
    });

    var wrap = make("label", "atlas-timeline");
    wrap.appendChild(make("span", "atlas-timeline-name", "Timeline"));
    var range = make("input");
    range.type = "range";
    range.min = "0";
    range.max = String(stops.length - 1);
    range.step = "1";
    range.value = range.max;
    var shown = make("output", "atlas-timeline-date", stops[stops.length - 1].label);
    range.addEventListener("input", function () {
      var stop = stops[Number(range.value)];
      shown.textContent = stop.label;
      wrap.classList.toggle("atlas-past", stop.value !== null);
      onChange(stop.value);
    });
    wrap.appendChild(range);
    wrap.appendChild(shown);
    controls.appendChild(wrap);
  }

  function focusing(figure, svg, holder) {
    var nodes = svg.querySelectorAll("[data-node]");
    if (!nodes.length) return { refresh: function () {} };

    var cards = figure.querySelector(".atlas-cards");
    if (cards) cards.hidden = true;
    var panel = make("aside", "atlas-panel");
    panel.setAttribute("aria-live", "polite");
    holder.parentNode.insertBefore(panel, holder.nextSibling);

    var current = null;
    var titles = {};
    each(nodes, function (node) { titles[node.getAttribute("data-node")] = nodeTitle(node); });

    function neighbours(id, gone) {
      var found = [];
      each(svg.querySelectorAll("[data-edge]"), function (edge) {
        var ends = words(edge.getAttribute("data-edge"));
        if (ends.indexOf(id) < 0) return;
        ends.forEach(function (end) {
          if (end !== id && !gone[end] && found.indexOf(end) < 0) found.push(end);
        });
      });
      return found;
    }

    function render(gone) {
      gone = gone || {};
      if (current && gone[current]) current = null;
      var near = current ? neighbours(current, gone) : [];

      each(nodes, function (node) {
        var id = node.getAttribute("data-node");
        node.classList.toggle("atlas-focused", id === current);
        node.classList.toggle("atlas-dim", !!current && id !== current && near.indexOf(id) < 0);
        node.setAttribute("aria-pressed", String(id === current));
      });
      each(svg.querySelectorAll("[data-edge]"), function (edge) {
        var touches = words(edge.getAttribute("data-edge")).indexOf(current) >= 0;
        edge.classList.toggle("atlas-dim", !!current && !touches);
      });

      panel.textContent = "";
      if (!current) {
        panel.appendChild(make("p", "atlas-panel-hint", "Select a box to see what it does and what it talks to."));
        return;
      }
      var card = cards && cards.querySelector('[data-card="' + current + '"]');
      if (card) {
        var copy = card.cloneNode(true);
        copy.removeAttribute("id");
        panel.appendChild(copy);
      } else {
        panel.appendChild(make("h3", null, titles[current]));
      }
      if (near.length) {
        panel.appendChild(make("p", "atlas-panel-links", "Talks to: " + near.map(function (id) { return titles[id] || id; }).join(", ")));
      }
      panel.appendChild(button("Clear", function () { select(null); }));
    }

    var lastGone = {};
    function select(id) {
      current = id === current ? null : id;
      render(lastGone);
    }

    each(nodes, function (node) {
      node.classList.add("atlas-node");
      node.setAttribute("tabindex", "0");
      node.setAttribute("role", "button");
      node.setAttribute("aria-label", nodeTitle(node));
      node.addEventListener("click", function (event) {
        event.stopPropagation();
        select(node.getAttribute("data-node"));
      });
      node.addEventListener("keydown", function (event) {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          select(node.getAttribute("data-node"));
        }
      });
    });
    svg.addEventListener("click", function () { if (current) select(null); });
    figure.addEventListener("keydown", function (event) {
      if (event.key === "Escape" && current) select(null);
    });

    return { refresh: function (gone) { lastGone = gone || {}; render(lastGone); } };
  }

  function stepping(figure, svg, controls) {
    var parts = svg.querySelectorAll("[data-step]");
    if (!parts.length) return;
    var list = figure.querySelector("ol.atlas-steps");
    var items = list ? list.querySelectorAll(":scope > li") : [];
    var last = 0;
    each(parts, function (part) { last = Math.max(last, Number(part.getAttribute("data-step")) || 0); });
    var step = 0;

    var group = make("div", "atlas-group atlas-stepper");
    group.setAttribute("role", "group");
    group.setAttribute("aria-label", "Steps");
    var previous = button("Previous", function () { go(step <= 1 ? 0 : step - 1); });
    var next = button("Next", function () { go(step >= last ? last : step + 1); });
    var all = button("Show all", function () { go(0); });
    var status = make("span", "atlas-step-status");
    status.setAttribute("aria-live", "polite");
    group.appendChild(previous);
    group.appendChild(next);
    group.appendChild(all);
    group.appendChild(status);
    controls.appendChild(group);
    var caption = make("p", "atlas-step-caption");
    controls.appendChild(caption);

    function go(target) {
      step = target;
      each(parts, function (part) {
        var n = Number(part.getAttribute("data-step"));
        part.classList.toggle("atlas-current", step > 0 && n === step);
        part.classList.toggle("atlas-future", step > 0 && n > step);
      });
      each(items, function (item, i) {
        if (step > 0 && i + 1 === step) item.setAttribute("aria-current", "step");
        else item.removeAttribute("aria-current");
      });
      previous.disabled = step === 0;
      next.disabled = step === last;
      all.disabled = step === 0;
      status.textContent = step === 0 ? "All " + last + " steps" : "Step " + step + " of " + last;
      caption.textContent = step > 0 && items[step - 1] ? items[step - 1].textContent.trim() : "";
      caption.hidden = !caption.textContent;
    }

    each(items, function (item, i) {
      item.classList.add("atlas-step-item");
      item.addEventListener("click", function () { go(i + 1); });
    });
    go(0);
  }

  function start() {
    var saved = load();
    each(document.querySelectorAll("figure.atlas-figure"), function (figure, i) { setup(figure, i, saved); });
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", start);
  else start();
})();
