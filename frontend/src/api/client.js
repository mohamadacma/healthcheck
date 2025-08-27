const RAW = process.env.REACT_APP_API_URL ?? 'http://localhost:5200';
export const API_BASE = RAW.replace(/\/+$/, '');    

export const getToken = () => localStorage.getItem('token');
export const setToken = (t) => localStorage.setItem('token', t);
export const clearToken = () => localStorage.removeItem('token');

const safeParse = (text) => {
    try
    {
        return JSON.parse(text);
    }
    catch 
    {
        return null; 
    }
}

export async function request(path, { method = 'GET', body, headers = {}, auth = true } = {}) {
    const token = auth ? getToken() : null;
  
    const h = {
      Accept: 'application/json',
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers,
    };
  
    const res = await fetch(`${API_BASE}${path}`, {
      method,
      headers: h,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  
    // Read body once:
    const text = await res.text();
    const ct = res.headers.get('content-type') || '';
    const isJson = ct.includes('application/json') || ct.includes('application/problem+json');
    const data = isJson && text ? safeParse(text) : null;
  
    if (!res.ok) {
      if (res.status === 401 && auth) {
        clearToken();
        window.dispatchEvent(new Event('app:unauthorized'));
      }
      const msg =
        (data && (data.detail || data.title || data.message || data.error)) ||
        text ||
        res.statusText;
  
      const err = new Error(`${res.status} ${msg}`);
      err.status = res.status;
      err.body = data ?? text;
      throw err;
    }
  
    if (res.status === 204) return null;
    return data ?? text ?? null;
  }
  

export const get = (path,options) => request(path, {...options,method:'GET'});
export const post =(path,body,options) => request(path,{...options, method:'POST',body});
export const put = (path,body,options) => request(path,{...options, method:'PUT',body});
export const del = (path,options) => request(path, {...options, method:'DELETE'});
