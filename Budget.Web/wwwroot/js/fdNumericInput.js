window.FDNumericInput = {
 // Attach by DOM element (if you already have the input element)
 attachKeyFilter: function (element) {
 var input = window.FDNumericInput._resolveInput(element);
 if (!input) return;
 window.FDNumericInput._wireHandlers(input);
 },

 // Attach by the Id set on the MudNumericField (Id may be on the input or a wrapper)
 attachKeyFilterById: function (id) {
 var el = document.getElementById(id);
 if (!el) return;
 var input = window.FDNumericInput._resolveInput(el);
 if (input) {
 window.FDNumericInput._wireHandlers(input);
 return;
 }
 // Fallback: if input not yet in DOM (Mud may render async), observe until it appears
 var observer = new MutationObserver(function (mutations, obs) {
 var target = window.FDNumericInput._resolveInput(el);
 if (target) {
 window.FDNumericInput._wireHandlers(target);
 obs.disconnect();
 }
 });
 observer.observe(el, { childList: true, subtree: true });
 },

 // Resolve the actual <input> element from either the input itself or a wrapper
 _resolveInput: function (el) {
 if (!el) return null;
 if (el.tagName && el.tagName.toLowerCase() === 'input') return el;
 var input = el.querySelector('input[type="text"], input[type="number"], input');
 return input || null;
 },

 // Wire keydown/beforeinput/input handlers
 _wireHandlers: function (input) {
 if (!input || input.dataset.fdnumWired === '1') return; // avoid duplicate wiring
 input.dataset.fdnumWired = '1';

 // Determine current locale decimal separator
 var sample = (1.1).toLocaleString();
 var decimalSep = sample.indexOf(',') > -1 ? ',' : '.';

 // Navigation keys set
 var navigationKeys = ['Backspace','Tab','ArrowLeft','ArrowRight','Delete','Home','End'];

 // Keydown filter (blocks most non-numeric keys at the key level)
 input.addEventListener('keydown', function (e) {
 // Allow modifiers (copy, paste shortcuts, etc.)
 if (e.ctrlKey || e.metaKey || e.altKey) return;

 var k = e.key;
 if (navigationKeys.indexOf(k) !== -1) return;
 if (k === '-' && input.selectionStart ===0 && input.value.indexOf('-') === -1) return; // single leading minus
 if (k === decimalSep) return; // locale decimal
 if ((k === '.' || k === ',') && decimalSep !== k) return; // allow alternate decimal; normalize later
 if (k >= '0' && k <= '9') return;

 e.preventDefault();
 });

 // Beforeinput: block illegal paste/IME insertions before they happen
 input.addEventListener('beforeinput', function (e) {
 try {
 // Some browsers provide e.data; for paste it may be null; handle generically
 var data = e.data != null ? e.data : '';
 // Build the would-be value
 var start = input.selectionStart ||0;
 var end = input.selectionEnd || start;
 var next = input.value.substring(0, start) + data + input.value.substring(end);
 if (!window.FDNumericInput._isPartialNumeric(next, decimalSep)) {
 e.preventDefault();
 }
 } catch { /* ignore */ }
 });

 // Input sanitizer (handles paste, IME, drops, etc.)
 input.addEventListener('input', function () {
 var v = input.value;
 // Normalize alternate decimal char to locale decimal
 if (decimalSep === ',') v = v.replace(/\./g, ',');
 else v = v.replace(/,/g, '.');

 // Keep only digits, one leading '-', and a single decimal separator
 var out = '';
 var hasMinus = false;
 var hasDec = false;
 for (var i =0; i < v.length; i++) {
 var ch = v[i];
 if (ch >= '0' && ch <= '9') { out += ch; continue; }
 if (ch === '-' && !hasMinus && out.length ===0) { hasMinus = true; out += ch; continue; }
 if (ch === decimalSep && !hasDec) { hasDec = true; out += ch; continue; }
 // drop anything else
 }

 // Allow partials: "-", ".", "-.", "123.", "-123."
 if (!window.FDNumericInput._isPartialNumeric(out, decimalSep)) {
 // Fallback: strip to digits only
 out = out.replace(/[^0-9]/g, '');
 }

 if (out !== input.value) input.value = out;
 });
 },

 // Validate partial numeric strings: allows empty, '-', '.', '-.', digits with optional leading '-' and single decimalSep
 _isPartialNumeric: function (s, decimalSep) {
 if (s === '' || s === '-' || s === decimalSep || s === ('-' + decimalSep)) return true;
 var esc = decimalSep === '.' ? '\\.' : decimalSep; // escape if '.'
 var re = new RegExp('^-?\\d*' + esc + '?\\d*$');
 return re.test(s);
 }
};
