// Shared helper functions used by all pages.
// important: uses Fetch with credentials to send cookies to the server.
// note: small helper functions include important/note/nota bene comments.
// nota bene: do not use window.alerts for status messages, use UI status areas.

(function(global){
  // important: returns unique id string
  function getUniqIdValue() {
    // note: GUID-like id; guaranteed unique for client-side element ids
    // nota bene: you can replace with other strategies if needed
    return 'id-' + cryptoRandomHex(16);
  }

  function cryptoRandomHex(bytes) {
    // small helper: generate secure hex string
    const arr = new Uint8Array(bytes);
    crypto.getRandomValues(arr);
    return Array.from(arr).map(b => b.toString(16).padStart(2,'0')).join('');
  }

  // show status messages in a provided container element or default to element id 'status'
  function showStatus(text, type='info', container) {
    const el = container || document.getElementById('status');
    if (!el) return;
    el.innerHTML = `<div class="alert alert-${normalizeType(type)} py-2" role="status">${escapeHtml(text)}</div>`;
  }

  function normalizeType(t) {
    if (!t) return 'secondary';
    if (['success','danger','warning','info','secondary','primary'].includes(t)) return t;
    return 'secondary';
  }

  // simple escape
  function escapeHtml(s){ return String(s||'').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;'); }

  // API helpers
  async function apiGet(path) {
    const res = await fetch(path, { credentials: 'include' });
    if (!res.ok) {
      const payload = await safeParse(res);
      throw new Error(payload?.error || payload?.message || res.statusText);
    }
    return await safeParse(res);
  }

  // options: { json:true } -> send JSON body; { formUrlEncoded:true } -> send urlencoded body
  async function apiPost(path, data, options={}) {
    const headers = {};
    let body = null;
    if (options.formUrlEncoded) {
      headers['Content-Type'] = 'application/x-www-form-urlencoded';
      body = data instanceof URLSearchParams ? data.toString() : new URLSearchParams(data).toString();
    } else if (options.json) {
      headers['Content-Type'] = 'application/json';
      body = JSON.stringify(data || {});
    } else {
      // default to form urlencoded when data is URLSearchParams
      headers['Content-Type'] = 'application/x-www-form-urlencoded';
      body = data instanceof URLSearchParams ? data.toString() : JSON.stringify(data);
    }

    const res = await fetch(path, {
      method: 'POST',
      credentials: 'include',
      headers,
      body
    });

    if (!res.ok) {
      const payload = await safeParse(res);
      throw payload || { message: res.statusText };
    }
    return await safeParse(res);
  }

  async function safeParse(res) {
    const txt = await res.text();
    try { return txt ? JSON.parse(txt) : null; } catch { return txt || null; }
  }

  // Modal-based confirmation (replaces window.confirm)
  // important: exposes showConfirm(message, title) -> Promise<boolean>
  // note: the page may include a #confirmModal element (recommended). If absent, one is created.
  function ensureConfirmModal() {
    let modalEl = document.getElementById('confirmModal');
    if (modalEl) return modalEl;

    // create modal markup dynamically if not present
    modalEl = document.createElement('div');
    modalEl.innerHTML = `
      <div class="modal fade" id="confirmModal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
          <div class="modal-content">
            <div class="modal-header">
              <h5 class="modal-title" id="confirmModalTitle">Please confirm</h5>
              <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <div class="modal-body" id="confirmModalBody"></div>
            <div class="modal-footer">
              <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
              <button type="button" class="btn btn-danger" id="confirmModalConfirmBtn">Confirm</button>
            </div>
          </div>
        </div>
      </div>`;
    document.body.appendChild(modalEl);
    return document.getElementById('confirmModal');
  }

  function showConfirm(message, title) {
    return new Promise((resolve) => {
      const modalEl = ensureConfirmModal();
      const modal = new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: true });
      const titleEl = modalEl.querySelector('#confirmModalTitle');
      const bodyEl = modalEl.querySelector('#confirmModalBody');
      const confirmBtn = modalEl.querySelector('#confirmModalConfirmBtn');

      titleEl.textContent = title || 'Please confirm';
      bodyEl.textContent = message || '';
      const cleanup = () => {
        confirmBtn.removeEventListener('click', onConfirm);
        modalEl.removeEventListener('hidden.bs.modal', onHidden);
      };
      const onConfirm = () => {
        cleanup();
        modal.hide();
        resolve(true);
      };
      const onHidden = () => {
        cleanup();
        resolve(false);
      };

      confirmBtn.addEventListener('click', onConfirm);
      modalEl.addEventListener('hidden.bs.modal', onHidden);
      modal.show();
    });
  }

  // Expose to global
  global.getUniqIdValue = getUniqIdValue;
  global.apiGet = apiGet;
  global.apiPost = apiPost;
  global.showStatus = (text, type='info', container) => showStatus(text, type, container);
  global.escapeHtml = escapeHtml;
  global.showConfirm = showConfirm;

})(window);