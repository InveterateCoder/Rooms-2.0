importScripts('/urls.js');
const CURRENT_CACHE = "rooms2.2.6";

const body = [
    "/static/js/main.52fba60e.chunk.js",
    "/static/js/main.52fba60e.chunk.js.map",
    "/static/js/runtime-main.7c45b422.js",
    "/static/js/runtime-main.7c45b422.js.map",
    "/static/js/2.0ff35695.chunk.js",
    "/static/js/2.0ff35695.chunk.js.map",
    "/static/js/2.0ff35695.chunk.js.LICENSE.txt",
    "/index.html",
    "/rooms-service-worker.js"
]

self.addEventListener("install", event => {
    event.waitUntil(caches.open(CURRENT_CACHE).then(cache => cache.addAll(body.concat(urls))));
});

self.addEventListener("activate", event => {
    event.waitUntil(
        caches.keys().then(cachesKeys => {
            return Promise.all(
                cachesKeys.map(cacheKey => {
                    if (cacheKey !== CURRENT_CACHE)
                        return caches.delete(cacheKey);
                })
            );
        })
    );
});

self.addEventListener('fetch', event => {
    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request);
        })
    );
});