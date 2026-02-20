import { useState } from 'react';

type MeResponse = {
  name: string;
  claims: Array<{ type: string; value: string }>;
};

export function App() {
  const [csrfToken, setCsrfToken] = useState<string>('');
  const [me, setMe] = useState<MeResponse | null>(null);
  const [message, setMessage] = useState<string>('');

  const getToken = async () => {
    const response = await fetch('/bff/antiforgery', { credentials: 'include' });
    const data = await response.json();
    setCsrfToken(data.token);
  };

  const login = async () => {
    await fetch('/bff/dev/login', { method: 'POST', credentials: 'include' });
    setMessage('Logged in as dev user');
  };

  const loadMe = async () => {
    const response = await fetch('/bff/me', { credentials: 'include' });
    if (response.status === 401) {
      setMessage('Unauthorized. Login first.');
      setMe(null);
      return;
    }

    setMe(await response.json());
  };

  const logout = async () => {
    await fetch('/bff/logout', {
      method: 'POST',
      credentials: 'include',
      headers: { 'X-CSRF-TOKEN': csrfToken }
    });
    setMe(null);
    setMessage('Logged out');
  };

  return (
    <main style={{ fontFamily: 'sans-serif', margin: '2rem' }}>
      <h1>Dadman Web BFF</h1>
      <p>Cookie-authenticated Web BFF demo UI.</p>
      <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
        <button onClick={getToken}>Get CSRF Token</button>
        <button onClick={login}>Dev Login</button>
        <button onClick={loadMe}>Load /bff/me</button>
        <button onClick={logout} disabled={!csrfToken}>Logout</button>
      </div>
      {message && <p><strong>{message}</strong></p>}
      <p>CSRF token: {csrfToken ? 'loaded' : 'not loaded'}</p>
      <pre>{JSON.stringify(me, null, 2)}</pre>
    </main>
  );
}
