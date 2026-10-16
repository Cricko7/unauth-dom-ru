"""Run against a disposable local database: creates one test account."""
import argparse
import html
from html.parser import HTMLParser
import http.cookiejar
import json
from pathlib import Path
import secrets
import urllib.parse
import urllib.request


class TokenParser(HTMLParser):
    token = None

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == 'input' and attrs.get('name') == '__RequestVerificationToken':
            self.token = attrs['value']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url', default='http://localhost:8080')
    parser.add_argument('--state-file', help='Save test credentials/cookies; rerun after restart to check persistence')
    args = parser.parse_args()
    base = args.url.rstrip('/')
    state_path = Path(args.state_file) if args.state_file else None
    previous = json.loads(state_path.read_text()) if state_path and state_path.exists() else None
    jar = http.cookiejar.LWPCookieJar(str(state_path) + '.cookies') if state_path else http.cookiejar.CookieJar()
    if previous:
        jar.load(ignore_discard=True)
    client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))

    def get(path):
        return client.open(base + path, timeout=30)

    def submit(path, fields):
        with get(path) as response:
            page = response.read().decode()
        tokens = TokenParser()
        tokens.feed(page)
        assert tokens.token, 'Antiforgery token is missing'
        fields['__RequestVerificationToken'] = tokens.token
        return client.open(base + path, urllib.parse.urlencode(fields).encode(), timeout=30)

    with get('/health') as response:
        assert json.load(response)['status'] == 'healthy'
    if previous:
        with get('/') as response:
            assert previous['phone'] in html.unescape(response.read().decode()), 'Session did not survive restart'
    for path in ['/', '/Wifi', '/Diagnost', '/SpeedTest', '/css/main.css', '/images/v268_7.png']:
        with get(path) as response:
            assert response.status == 200 and response.read(), path
    with get('/api/speedtest/download?mb=1') as response:
        assert len(response.read()) == 1024 * 1024
    payload = secrets.token_bytes(4096)
    request = urllib.request.Request(base + '/api/speedtest/upload', data=payload,
                                     headers={'Content-Type': 'application/octet-stream'})
    with client.open(request, timeout=30) as response:
        assert json.load(response)['received'] == len(payload)
    phone = previous['phone'] if previous else '+79' + ''.join(str(secrets.randbelow(10)) for _ in range(9))
    password = previous['password'] if previous else secrets.token_hex(16)
    if not previous:
        with submit('/Auth/Register', {'Input.Phone': phone, 'Input.Password': password,
                                      'Input.ConfirmPassword': password}) as response:
            assert urllib.parse.urlparse(response.url).path in ['/', '/Index'], 'Registration failed'
            assert phone in html.unescape(response.read().decode()), 'Registered user missing'
    jar.clear()
    with submit('/Auth/Login', {'Input.Phone': phone, 'Input.Password': password}) as response:
        assert urllib.parse.urlparse(response.url).path in ['/', '/Index'], 'Login failed'
        assert phone in html.unescape(response.read().decode()), 'Authenticated user missing'
    print('PASS: health, pages, static assets, download/upload, registration and login')
    if state_path:
        state_path.touch(mode=0o600, exist_ok=True)
        state_path.write_text(json.dumps({'phone': phone, 'password': password}))
        jar.save(ignore_discard=True)
        Path(str(state_path) + '.cookies').chmod(0o600)
    print('PASS: session and account survived restart' if previous else 'One test account was created in the database.')


if __name__ == '__main__':
    main()
