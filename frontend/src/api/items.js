import {get,post ,put, del } from './client';



export const getItem = (id) => get(`/items/${id}`);

export function listItems(params = {}) {
    const qs = new URLSearchParams(
        Object.entries(params)
            .filter(([, v]) => v !== undefined && v !== null && v !=='')
    ).toString();

    return get(`/items${qs ? `?${qs}` : ''}`);
}
export const createItem = (payLoad) => post('/items', payLoad);
export const updateItem = (id, payload) => put(`/items/${id}`, payload);
export async function deleteItem(id) { 
    del(`/items/${id}`);

}