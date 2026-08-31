const TOKEN_KEY = "cms.jwt";
const API_BASE_KEY = "cms.apiBaseUrl";

export function getToken() {
  return localStorage.getItem(TOKEN_KEY) || "";
}

export function setToken(token) {
  if (!token) {
    localStorage.removeItem(TOKEN_KEY);
    return;
  }

  localStorage.setItem(TOKEN_KEY, token);
}

export function getApiBaseUrl() {
  return localStorage.getItem(API_BASE_KEY) || "https://localhost:7071";
}

export function setApiBaseUrl(url) {
  if (!url) {
    localStorage.removeItem(API_BASE_KEY);
    return;
  }

  localStorage.setItem(API_BASE_KEY, url.replace(/\/$/, ""));
}

export async function apiRequest(path, options = {}) {
  const baseUrl = getApiBaseUrl();
  const token = getToken();

  const headers = {
    "Content-Type": "application/json",
    ...(options.headers || {})
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const response = await fetch(`${baseUrl}${path}`, {
    ...options,
    headers
  });

  if (!response.ok) {
    const bodyText = await response.text();
    const message = bodyText || `Request failed with ${response.status}`;
    const err = new Error(message);
    err.status = response.status;
    throw err;
  }

  if (response.status === 204) {
    return null;
  }

  return response.json();
}

export async function login(email, password) {
  return apiRequest("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password })
  });
}

export async function queryCollection(path, query) {
  const params = new URLSearchParams();

  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") {
      params.set(key, String(value));
    }
  });

  const suffix = params.toString() ? `?${params.toString()}` : "";
  return apiRequest(`${path}${suffix}`);
}
