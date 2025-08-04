import React, { useState, useSyncExternalStore } from "react";
import { createItem } from '../api/items';

export default function ItemForm({ onCreated }) {
    const [name, setName] = useState('');
    const [quantity, setQuantity] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');

    async function handleSubmit(e) {
        e.preventDefault();
        setLoading(true);
        setError('');
        setSuccess('');

        if(!name.trim()) {
            setLoading(false);
            setError('Name is required');
            return;
        }
        const qty = Number(quantity);
        if (!Number.isFinite(qty) || qty < 0) {
            setLoading(false);
            setError('Quantity must be a non-negative number');
            return;
        }

        try {
            const created = await createItem({ name: name.trim(), quantity: qty });
            setSuccess(`Created: ${created.name} (id ${created.id})`);
            setName('');
            setQuantity('');
            onCreated?.(created);
        } catch (err) {
            setError(err.message || 'Failed to create item');
        } finally {
            setLoading(false);
        }
    }

    return (
        <form onSubmit={handleSubmit} style={{ marginTop: 16, marginBottom: 8, display: 'flex', gap: 8}}>
            <input 
                placeholder="Item name"
                value = {name}
                onChange = {(e) => setName(e.target.value)}
            />
            <input 
                placeholder="Quantity"
                value = {quantity}
                onChange={(e) => setQuantity(e.target.value)}
                inputMode="numeric"
            />
            <button type="submit" disabled={loading}>{loading ? 'Saving...': 'Add Item'}</button>

            {error && <p style={{ color: 'crimson', marginLeft: 12}}>Error : {error}</p>}
            {success && <p style={{ color: 'green', marginLeft: 12}}>{success}</p>}
        </form>
    );
}