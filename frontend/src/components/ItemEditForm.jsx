import React, { useState } from 'react';
import { updateItem } from '../api/items';

export default function ItemEditForm({ item, onUpdated, onCancel }) {
   
    const [name, setName] = useState(item?.name || '');
    const [quantity, setQuantity] = useState(item?.quantity ?? 0);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
    const [loading, setLoading] = useState(false);
  
    if (!item) return <p>No item selected to edit.</p>;
  
    const onSubmit = async (e) => {
      e.preventDefault();
      setError('');
      setSuccess('');
      setLoading(true);
      try {
        const result = await updateItem(item.id, {
          name: name.trim(),
          quantity: Number(quantity)
        });
        setSuccess(`Updated item ${result.name} (ID: ${result.id})`);
        onUpdated?.();
      } catch (err) {
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
  
    return (
      <form onSubmit={onSubmit} style={{ display:'flex', gap:8, alignItems:'center' }}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="New name" required />
        <input
          type="number"
          min="0"
          step="1"
          value={quantity}
          onChange={(e) => setQuantity(Number(e.target.value))}
          placeholder="Quantity"
          required
        />
        <button type="submit" disabled={loading}>
          {loading ? 'Saving…' : 'Update'}
          </button>
          {onCancel && (
            <button type="button" onClick={onCancel} disabled={loading}>
              Cancel
            </button>
          )}
        {error && <p style={{ color: 'red' }}>Error: {error}</p>}
        {success && <p style={{ color: 'green' }}>{success}</p>}
      </form>
    );
  }
  