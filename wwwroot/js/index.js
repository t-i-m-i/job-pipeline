// Live search for Pages/Index.cshtml (plain JS, no framework).
//
// Idea: the page already answers GET /?searchquery=acme&status=Applied with full HTML.
// So we fetch that same URL in the background and swap the changed pieces into the page.
// Node/Next.js analogy: like router.replace(`?${params}`), except the server returns
// rendered HTML instead of JSON, so there is no endpoint to add.

const form = document.querySelector('#filter-form');
const input = form.querySelector('input[name="searchquery"]');

// Pieces of the page that depend on the search result. Each is replaced with the
// same element from the fresh response. The input itself is never replaced,
// which is why it keeps focus and the caret while you type.
const swapIds = ['applications-count', 'search-clear', 'applications-table'];

let timer;      // pending debounce timeout
let inflight;   // AbortController of the request currently running

async function refresh() {
    // 1. Build the URL from whatever is in the form right now (searchquery + status).
    const params = new URLSearchParams(new FormData(form));
    for (const [key, value] of [...params]) {
        if (!value) params.delete(key); // keep the URL tidy: no "?searchquery=&status="
    }
    const query = params.toString();
    const url = query ? `${location.pathname}?${query}` : location.pathname;

    // 2. Cancel the previous request so a slow, older response can't overwrite a newer one.
    inflight?.abort();
    inflight = new AbortController();

    try {
        const response = await fetch(url, { signal: inflight.signal });
        if (!response.ok) return;

        // 3. Parse the returned HTML into a detached document and copy the pieces over.
        const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
        for (const id of swapIds) {
            document.getElementById(id).replaceWith(doc.getElementById(id));
        }

        // 4. Keep the address bar in sync (refresh/bookmark/share still work),
        //    without adding a history entry for every keystroke.
        history.replaceState(null, '', url);
    } catch (err) {
        if (err.name !== 'AbortError') throw err; // aborting is expected, anything else is not
    }
}

// Debounce: wait until typing pauses for 300ms, then search once.
input.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(refresh, 300);
});

// Enter / "Search" button: search right away, without a full page reload.
form.addEventListener('submit', (event) => {
    event.preventDefault();
    clearTimeout(timer);
    refresh();
});
