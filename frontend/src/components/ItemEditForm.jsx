import React, { useState } from 'react';
import { updateItem } from '../api/items';

export default function ItemEditForm({ item, onUpdated }) {
   
    const [name, setName] = useState(item?.name || '');
    const [quantity, setQuantity] = useState(item?.quantity || 0);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');
  
    if (!item) return <p>No item selected to edit.</p>;
  
    const onSubmit = async (e) => {
      e.preventDefault();
      setError('');
      try {
        const result = await updateItem(item.id, { name, quantity });
        setSuccess(`Updated item ${result.name} (ID: ${result.id})`);
        onUpdated();
      } catch (err) {
        setError(err.message);
      }
    };
  
    return (
      <form onSubmit={onSubmit}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="New name" />
        <input
          type="number"
          value={quantity}
          onChange={(e) => setQuantity(Number(e.target.value))}
          placeholder="Quantity"
        />
        <button type="submit">Update</button>
        {error && <p style={{ color: 'red' }}>Error: {error}</p>}
        {success && <p style={{ color: 'green' }}>{success}</p>}
      </form>
    );
  }
  