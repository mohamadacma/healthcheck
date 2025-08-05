import {get} from './client';
import {post} from './client';

export const getItem = (id) => get(`/items/${id}`)
export function listItems(params = {}) {
    const qs = new URLSearchParams(
        Object.entries(params)
            .filter(([, v]) => v !== undefined && v !== null && v !=='')
    ).toString();

    return get(`/items${qs ? `?${qs}` : ''}`);
}
export const createItem = (payLoad) => post('/items', payLoad);
export async function updateItem(id, updatedItem) {
    const token = localStorage.getToken('token');
    const res = await fetch(`http://localhost:5200/items/${id}`, {
        method : 'PUT',
        headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(updatedItem),
    });

    if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || 'Failed to update item');
    }

    return res.json();
}