import React, { useEffect, useMemo, useState } from 'react';
import { listItems } from '../api/items';
import { deleteItem } from '../api/items';

function useDebounced(value, delay = 400) {
    const [v, setV] = useState(value);
    useEffect(() => {
        const t = setTimeout(() => setV(value), delay);
        return () => clearTimeout(t);
    }, [value, delay]);
    return v;
}

export default function ItemList({ refreshKey = 0 }) {
    const [items, setItems] = useState([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [search, setSearch] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');

    const DebouncedSearch = useDebounced(search, 400);
    const totalPages = useMemo(
        () => Math.max(1, Math.ceil(total / pageSize)),
        [total, pageSize]
    );

    useEffect(() => {
        let cancelled = false; 
        async function run() {
            setLoading(true);
            setError('');
            try {
                const data = await listItems({ search: DebouncedSearch, page, pageSize });
                if (!cancelled) {
                    setItems(data.items || []);
                    setTotal(data.total ?? 0);
                }
            } catch (err) {
                if (!cancelled) setError(err.message || 'Failed to load items');
            } finally {
                if(!cancelled) setLoading(false)
            }
        }
        run();
        return () => { cancelled = true; };
    }, [DebouncedSearch, page, pageSize, refreshKey]);

    const canPrev = page > 1;
    const canNext = page < totalPages;

    //DELETE
    async function handleDelete(id) {
        const ok = window.confirm('Delete this item? This cannot be undone. ')
        if(!ok) return;

        const prev = items;
        setItems(prev => prev.filter(i =>i.id !==id));
        setTotal(t =>  Math.max(0, t-1));

        try {
            await deleteItem(id);
           
        } catch (err) {
            setItems(prev);
            setTotal(prev => prev +1);
            alert (err.message || 'Failed to delete item');
        }
    }

    return (
        <section style ={{ marginTop: 24}}>
            <h2>Inventory</h2>

            <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 12}}>
                <input 
                placeholder="Search by name..."
                value={search}
                onChange={e => { setSearch(e.target.value); setPage(1); }}
                />
                <label>
                    Page size: {' '}
                    <select value= {pageSize} onChange={e => {setPageSize(Number(e.target.value)); setPage(1); }}>
                        {[5,10,20,50]. map(n => <option key={n} value={n}>{n}</option>)}
                    </select>
                </label>
                <span style= {{ marginLeft: 'auto' }}>
                    {total} total • page {page} / {totalPages}
                </span>
            </div>

            {loading && <p>Loading...</p>}
            {error && <p style={{ color: 'crimson' }}>Error: {error}</p>}
            {!loading && !error && items.length === 0 && <p>No items found.</p>}

            {!loading && !error && items.length > 0 && (
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                    <thead>
                        <tr>
                            <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 6}}>ID</th>
                            <th style={{ textAlign:'left', borderBottom: '1px solid #ddd', padding: 6}}> Name</th>
                            <th style={{ textAlign:'left', borderBottom: '1px solid #ddd', padding: 6}}>Quantity</th>
                            <th style={{ textAlign:'left', borderBottom: '1px solid #ddd', padding: 6 }}>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {items.map(i => (
                            <tr key={i.id}>
                                <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6}}>{i.id}</td>
                                <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6}}>{i.name}</td>
                                <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6}}>{i.quantity}</td>
                                <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6 }}>
                                    <button onClick={() => handleDelete(i.id)}>Delete</button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}

            <div style={{ marginTop: 12, display: 'flex', gap: 8}}>
                <button onClick={() => setPage(p =>Math.max(1,p-1))} disabled={!canPrev}>Prev</button>
                <button onClick={() => setPage(p =>Math.min(totalPages, p+1))} disabled={!canNext}>Next</button>
            </div>
        </section>

    );

}
