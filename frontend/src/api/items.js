import {get,post ,put, del } from './client';



export const getItem = (id) => get(`/items/${id}`);

export async function listItems({ search = '', page = 1, pageSize = 10, minQuantity, maxQuantity } = {}) {
    const params = new URLSearchParams({
      search,
      page: String(page),
      pageSize: String(pageSize),
    });
    
    if (minQuantity !== undefined && minQuantity !== '') {
      params.append('minQuantity', String(minQuantity));
    }
    if (maxQuantity !== undefined && maxQuantity !== '') {
      params.append('maxQuantity', String(maxQuantity));
    }
    
    return get(`/items?${params.toString()}`);
  }

  export async function deductUsage(id, data) {
    const token = localStorage.getItem('token'); // From login
    const response = await fetch(`http://localhost:5200/items/${id}/deduct`, { 
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
      },
      body: JSON.stringify(data), // { amount, reason, user }
    });
    if (!response.ok) throw new Error('Deduction failed');
    return response.json();
  }
  
export const createItem = (payLoad) => post('/items', payLoad);
export const updateItem = (id, payload) => put(`/items/${id}`, payload);
export const deleteItem = (id) =>  del(`/items/${id}`);