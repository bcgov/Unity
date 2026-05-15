TimezoneUtils.setTimezoneCookie();

// Only reload if the cookie was actually persisted — prevents an infinite reload
// loop if the browser silently rejects the cookie (e.g. strict privacy settings).
const cookieSet = document.cookie.split('; ').some(row => row.startsWith('timezoneoffset='));
if (cookieSet) {
    location.reload();
}
