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