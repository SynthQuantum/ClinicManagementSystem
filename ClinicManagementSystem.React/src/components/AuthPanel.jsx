import { useState } from "react";
import { getApiBaseUrl, getToken, login, setApiBaseUrl, setToken } from "../api/client";

export default function AuthPanel() {
  const [apiBaseUrl, setLocalApiBaseUrl] = useState(getApiBaseUrl());
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [token, setLocalToken] = useState(getToken());
  const [status, setStatus] = useState("");

  async function handleLogin(event) {
    event.preventDefault();
    setStatus("Logging in...");

    try {
      setApiBaseUrl(apiBaseUrl);
      const result = await login(email, password);
      setToken(result.token);
      setLocalToken(result.token);
      setStatus("Login successful. JWT saved for API calls.");
      setPassword("");
    } catch (error) {
      setStatus(`Login failed: ${error.message}`);
    }
  }

  function handleSaveApi() {
    setApiBaseUrl(apiBaseUrl);
    setStatus("API base URL saved.");
  }

  function clearToken() {
    setToken("");
    setLocalToken("");
    setStatus("JWT token cleared.");
  }

  return (
    <section className="card auth-card">
      <h2>Connection and Auth</h2>
      <div className="inline-row">
        <label htmlFor="apiBaseUrl">API base URL</label>
        <input
          id="apiBaseUrl"
          value={apiBaseUrl}
          onChange={(e) => setLocalApiBaseUrl(e.target.value)}
          placeholder="https://localhost:7071"
        />
        <button type="button" onClick={handleSaveApi}>
          Save URL
        </button>
      </div>

      <form className="auth-form" onSubmit={handleLogin}>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="email"
          required
        />
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="password"
          required
        />
        <button type="submit">Login and Store JWT</button>
        <button type="button" className="muted" onClick={clearToken}>
          Clear Token
        </button>
      </form>

      <p className="status">{status}</p>
      <p className="token-preview">Token loaded: {token ? "Yes" : "No"}</p>
    </section>
  );
}
