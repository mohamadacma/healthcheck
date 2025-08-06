const API_BASE = process.env.REACT_APP_API_URL || 'http://localhost:5200';

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

export async function request(path, {method = 'GET', body, headers={}, auth= true})
{
    const h = {'Content-Type': 'application/json', ...headers };
    if(auth)
    {
        const t = getToken();
        if(t) h['Authorization'] = `Bearer ${t}`;
    }

    const res = await fetch(`${API_BASE}${path}`, {
        method,
        headers : h,
        body : body ? JSON.stringify(body) : undefined,
    });

    const text = await res.text();
    const data = text ? safeParse(text) : null;

    if(!res.ok) {
        if (res.status === 401) { clearToken(); }
        const msg = (data && (data.title || data.detail)) || text || res.statusText;
        const err = new Error(`${res.status} ${msg}`);
        err.status = res.status;
        err.body = data || text;
        throw err;
    }
    return data;
}

export const get = (path,options) => request(path, {...options,method:'GET'});
export const post =(path,body,options) => request(path,{...options, method:'POST',body});
export const put = (path,body,options) => request(path,{...options, method:'PUT',body});
export const del = (path,options) => request(path, {...options, method:'DELETE'});
