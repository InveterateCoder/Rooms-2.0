const CURRENT_CACHE = '1';

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheKeys => {
            return Promise.all(
                cacheKeys.map(cacheKey => {
                    if (cacheKey !== CURRENT_CACHE)
                        return caches.delete(cacheKey);
                })
            );
        })
    );
});

self.addEventListener('fetch', event => {
    if (event.request.url.startsWith(new URL('api', location.origin))
        || event.request.url.startsWith(new URL('hubs', location.origin)))
        event.respondWith(fetch(event.request));
    else
        event.respondWith(
            caches.match(event.request).then(response => {
                return response || fetch(event.request).then(response => {
                    return caches.open(CURRENT_CACHE).then(cache => {
                        cache.put(event.request, response.clone());
                        return response;
                    });
                });
            })
        );
});