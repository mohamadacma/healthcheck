import React, { useEffect, useMemo, useState } from 'react';
import { listItems, deleteItem } from '../api/items';
import ItemEditForm from './ItemEditForm';

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
    const [editingId, setEditingId] = useState(null);
    const [localRefresh, setLocalRefresh] = useState(0);
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
    const hasItems = items.length > 0;
    const isSearching = !!DebouncedSearch?.trim();
    const showEmpty = !loading && !error && !hasItems;
    const canPrev = page > 1;
    const canNext = page < totalPages;

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

    //DELETE
    async function handleDelete(id) {
        const ok = window.confirm('Delete this item? This cannot be undone. ')
        if(!ok) return;

        const prevItems = items;
        const prevTotal = total;

        setItems(prev => prev.filter(i =>i.id !==id));
        setTotal(t =>  Math.max(0, t-1));

        try {
            await deleteItem(id);
           
        } catch (err) {
            setItems(prevItems);
            setTotal(prevTotal);
            alert (err.message || 'Failed to delete item');
        }
    }

    return (
        <section style={{ marginTop: 24 }}>
          <h2>Inventory</h2>
    
          <div style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 12 }}>
            <input
              placeholder="Search by name..."
              value={search}
              onChange={e => { setSearch(e.target.value); setPage(1); }}
            />
            <button
              type="button"
              onClick={() => { setSearch(''); setPage(1); }}
              disabled={!search}
              title="Clear search"
              style={{ padding: '6px 10px' }}
            >
              Clear
            </button>
            <label>
              Page size:{' '}
              <select
                value={pageSize}
                onChange={e => { setPageSize(Number(e.target.value)); setPage(1); }}
                disabled={total === 0}
              >
                {[5, 10, 20, 50].map(n => <option key={n} value={n}>{n}</option>)}
              </select>
            </label>
            <span style={{ marginLeft: 'auto' }}>
              {total} total • page {page} / {totalPages}
            </span>
          </div>
    
          {loading && <p>Loading...</p>}
          {error && <p style={{ color: 'crimson' }}>Error: {error}</p>}
          {showEmpty && (
            isSearching
              ? <p>No results for “{DebouncedSearch}”. <button onClick={() => { setSearch(''); setPage(1); }}>Clear search</button></p>
              : <p>No items yet. Use <strong>Add Item</strong> above to create your first one.</p>
          )}
    
          {!loading && !error && hasItems && (
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr>
                  <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 6 }}>ID</th>
                  <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 6 }}>Name</th>
                  <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 6 }}>Quantity</th>
                  <th style={{ textAlign: 'left', borderBottom: '1px solid #ddd', padding: 6 }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map(i => (
                  <React.Fragment key={i.id}>
                    <tr>
                      <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6 }}>{i.id}</td>
                      <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6 }}>{i.name}</td>
                      <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6 }}>{i.quantity}</td>
                      <td style={{ borderBottom: '1px solid #f0f0f0', padding: 6 }}>
                        <button onClick={() => setEditingId(prev => prev === i.id ? null : i.id)}>
                          {editingId === i.id ? 'Close' : 'Edit'}
                        </button>{' '}
                        <button onClick={() => handleDelete(i.id)}>Delete</button>
                      </td>
                    </tr>
                    {editingId === i.id && (
                      <tr>
                        <td colSpan={4} style={{ padding: 8, background: '#fafafa' }}>
                          <ItemEditForm
                            item={i}
                            onUpdated={() => { setEditingId(null); setLocalRefresh(x => x + 1); }}
                            onCancel={() => setEditingId(null)}
                          />
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
                ))}
              </tbody>
            </table>
          )}
    
          <div style={{ marginTop: 12, display: 'flex', gap: 8 }}>
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={!canPrev}>Prev</button>
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={!canNext}>Next</button>
          </div>
        </section>
      );
    }
