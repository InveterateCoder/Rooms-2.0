const CURRENT_CACHE = "rooms2.2";

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
    if (event.request.url.startsWith('http') && event.request.method === 'GET'
        && !event.request.url.startsWith(new URL('api', location.origin))
        && !(event.request.cache === 'only-if-cached' && event.request.mode !== 'same-origin'))
    {
        caches.match(event.request).then(response => {
            return response || fetch(event.request).then(response => {
                return caches.open(CURRENT_CACHE).then(cache => {
                    cache.put(event.request, response.clone());
                    return response;
                });
            });
        });
    }
});