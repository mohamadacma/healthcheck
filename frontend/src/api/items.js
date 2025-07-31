import {get} from './client';

export const getItem = (id) => get(`/items/${id}`)