import React, { useState } from "react";
import { getItem } from "../api/items";

export default function ItemLookup() {
    const [id, setId] = useState('1');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [item, setItem] = useState(null);
    const [success, setSuccess] = useState('');

    const onFetch = async (e) => {
        e.preventDefault();
        setLoading(true); setError(''); setItem(null);
        setSuccess(`Item ID ${id} loaded successfully ✅`);
        try {
            const data = await getItem(id);
            setItem(data);
        } catch (err) {
            setError(err.message);
            setSuccess('');
        } finally {
            setLoading(false);
        }
    };

    return (
        <div>
            <form onSubmit={onFetch} style={{marginBottom: 12}}>
                <input value= {id} onChange={e => setId(e.target.value)} placeholder="Item ID" />
                <button type = "submit">Fetch</button>
            </form>

            {loading && <p>Loading...</p>}
            {success && <p style={{color:'green'}}>{success}</p>}
            {error && <p style={{color: 'crimson'}}>Error: {error}</p>}
            {item   && (
                <pre style={{background: '#f6f8fa', padding:12}}>
                    {JSON.stringify(item,null,2)}
                </pre>
            )}
        </div>
    );
}