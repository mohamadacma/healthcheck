import React, { useState, useEffect } from 'react';
import { getItem } from '../api/items';
import { listItems } from '../api/items';
import { deleteItem } from '../api/items';

// API functions 
const API_BASE = process.env.REACT_APP_API_URL || 'http://localhost:5200';

const getToken = () => localStorage.getItem('token');
const clearToken = () => localStorage.removeItem('token');

const safeParse = (text) => {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
};

async function request(path, { method = 'GET', body, headers = {}, auth = true } = {}) {
  const h = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
    ...headers,
  };

  if (auth) {
    const t = getToken();
    if (t) h['Authorization'] = `Bearer ${t}`;
  }

  const res = await fetch(`${API_BASE}${path}`, {
    method,
    headers: h,
    body: body ? JSON.stringify(body) : undefined,
  });

  const contentType = res.headers.get('content-type') || '';
  const isJson = contentType.includes('application/json');
  const text = await res.text();
  const data = isJson && text ? safeParse(text) : null;

  if (!res.ok) {
    if (res.status === 401) {
      clearToken();
      window.dispatchEvent(new Event('app:unauthorized'));
    }

    const msg = (data && (data.title || data.detail || data.message || data.error)) || text || res.statusText;
    const err = new Error(`${res.status} ${msg}`);
    err.status = res.status;
    err.body = data || text;
    throw err;
  }
  return data;
}

const get = (path, options) => request(path, { ...options, method: 'GET' });
const del = (path, options) => request(path, { ...options, method: 'DELETE' });

// Custom hook for debounced values
function useDebounced(value, delay = 400) {
  const [v, setV] = useState(value);
  useEffect(() => {
    const t = setTimeout(() => setV(value), delay);
    return () => clearTimeout(t);
  }, [value, delay]);
  return v;
}

const HospitalInventorySearch = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filters, setFilters] = useState({
    category: '',
    minQuantity: '',
    maxQuantity: '',
    location: '',
    lowStock: false,
    expiringSoon: false
  });
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [results, setResults] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  
  const debouncedSearch = useDebounced(searchTerm, 400);
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  const categories = [
    "All Categories",
    "Medications",
    "Medical Devices", 
    "PPE",
    "Surgical Instruments",
    "Diagnostic Equipment",
    "Consumables"
  ];

  const locations = [
    "All Locations",
    "Pharmacy",
    "ICU",
    "Emergency Department",
    "Operating Room",
    "General Ward",
    "Pediatrics",
    "Maternity"
  ];

  const handleSearch = async () => {
    setLoading(true);
    setError('');
    
    try {
      const searchParams = {
        search: debouncedSearch,
        page,
        pageSize,
        minQuantity: filters.minQuantity || undefined,
        maxQuantity: filters.maxQuantity || undefined,
      };

      const response = await listItems(searchParams);
      console.log('API response:', response);
      setResults(response.data || []);
      setTotal(response.totalCount || 0);
    } catch (err) {
      setError(err.message || 'Failed to load items');
      setResults([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  // Handle delete with optimistic updates
  const handleDelete = async (id) => {
    const ok = window.confirm('Delete this item? This cannot be undone.');
    if (!ok) return;

    const prevItems = results;
    const prevTotal = total;

    // Optimistic update
    setResults(prev => prev.filter(i => i.id !== id));
    setTotal(t => Math.max(0, t - 1));

    try {
      await deleteItem(id);
    } catch (err) {
      // Revert on error
      setResults(prevItems);
      setTotal(prevTotal);
      alert(err.message || 'Failed to delete item');
    }
  };

  const clearFilters = () => {
    setSearchTerm('');
    setFilters({
      category: '',
      minQuantity: '',
      maxQuantity: '',
      location: '',
      lowStock: false,
      expiringSoon: false
    });
    setPage(1);
    setResults([]);
    setTotal(0);
  };

  // Quick search by ID
  const handleQuickSearchById = async (id) => {
    if (!id || !id.trim()) return;
    
    setLoading(true);
    setError('');
    
    try {
      const item = await getItem(id.trim());
      setResults([item]);
      setTotal(1);
      setSearchTerm('');
    } catch (err) {
      setError(err.message || 'Item not found');
      setResults([]);
      setTotal(0);
    } finally {
      setLoading(false);
    }
  };

  const handleQuickSearch = (type) => {
    switch(type) {
      case 'lowStock':
        setFilters(prev => ({ ...prev, lowStock: true, maxQuantity: '50' })); 
        break;
      case 'expiring':
        setFilters(prev => ({ ...prev, expiringSoon: true }));
        break;
      case 'medications':
        setFilters(prev => ({ ...prev, category: 'Medications' }));
        break;
    }
    setPage(1);
  };

  // Search effect with debouncing
  useEffect(() => {
    if (debouncedSearch || Object.values(filters).some(v => v)) {
      handleSearch();
    } else if (!debouncedSearch && !Object.values(filters).some(v => v)) {
      setResults([]);
      setTotal(0);
    }
  }, [debouncedSearch, filters, page, pageSize]);

  // Reset page when search changes
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, filters.category, filters.location, filters.minQuantity, filters.maxQuantity]);

  const hasResults = results.length > 0;
  const isSearching = !!debouncedSearch?.trim() || Object.values(filters).some(v => v);
  const showEmpty = !loading && !error && !hasResults;

  return (
    <div style={{ 
      backgroundColor: '#f8f9fa', 
      padding: '24px', 
      borderRadius: '8px', 
      border: '1px solid #e9ecef',
      marginBottom: '24px'
    }}>
      <h3 style={{ margin: '0 0 16px 0', color: '#1a5490' }}>
        🔍 Hospital Inventory Search
      </h3>

      {/* Main Search Bar */}
      <div style={{ display: 'flex', gap: '16px', marginBottom: '16px' }}>
        <div style={{ flex: 1 }}>
          <input
            type="text"
            placeholder="Search by item name or enter ID for specific item..."
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ced4da',
              borderRadius: '4px',
              fontSize: '14px'
            }}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            onKeyPress={(e) => {
              if (e.key === 'Enter' && /^\d+$/.test(searchTerm.trim())) {
                handleQuickSearchById(searchTerm);
              }
            }}
          />
        </div>
        <button
          onClick={() => setShowAdvanced(!showAdvanced)}
          style={{
            background: 'none',
            border: '1px solid #6c757d',
            color: '#6c757d',
            padding: '8px 16px',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px'
          }}
        >
          {showAdvanced ? '▼ Hide Advanced Options' : '▶ Show Advanced Options'}
        </button>
        <button
          onClick={clearFilters}
          style={{
            backgroundColor: '#dc3545',
            color: 'white',
            border: 'none',
            padding: '8px 16px',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px',
            opacity: !isSearching ? 0.5 : 1,
            pointerEvents: !isSearching ? 'none' : 'auto'
          }}
        >
          Clear All
        </button>
      </div>

      {/* Quick Action Buttons */}
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '8px', marginBottom: '20px' }}>
        <button
          onClick={() => handleQuickSearch('lowStock')}
          style={{
            backgroundColor: '#f8d7da',
            color: '#dc3545',
            border: 'none',
            padding: '8px 16px',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px'
          }}
        >
          ⚠️ Low Stock Items
        </button>
        <button
          onClick={() => handleQuickSearch('expiring')}
          style={{
            backgroundColor: '#fff3cd',
            color: '#ffc107',
            border: 'none',
            padding: '8px 16px',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px'
          }}
        >
          📅 Expiring Soon
        </button>
        <button
          onClick={() => handleQuickSearch('medications')}
          style={{
            backgroundColor: '#d1ecf1',
            color: '#0c5460',
            border: 'none',
            padding: '8px 16px',
            borderRadius: '4px',
            cursor: 'pointer',
            fontSize: '14px'
          }}
        >
          💊 Medications
        </button>
      </div>

      {/* Advanced Filters */}
      {showAdvanced && (
        <div style={{ 
          padding: '16px', 
          backgroundColor: '#ffffff', 
          borderRadius: '4px', 
          border: '1px solid #dee2e6',
          marginBottom: '20px'
        }}>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
            gap: '16px' 
          }}>
            <div>
              <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                Category
              </label>
              <select
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ced4da',
                  borderRadius: '4px',
                  fontSize: '14px'
                }}
                value={filters.category}
                onChange={(e) => setFilters(prev => ({ ...prev, category: e.target.value }))}
              >
                {categories.map(cat => (
                  <option key={cat} value={cat === "All Categories" ? "" : cat}>
                    {cat}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                Location
              </label>
              <select
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ced4da',
                  borderRadius: '4px',
                  fontSize: '14px'
                }}
                value={filters.location}
                onChange={(e) => setFilters(prev => ({ ...prev, location: e.target.value }))}
              >
                {locations.map(loc => (
                  <option key={loc} value={loc === "All Locations" ? "" : loc}>
                    {loc}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                Min Quantity
              </label>
              <input
                type="number"
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ced4da',
                  borderRadius: '4px',
                  fontSize: '14px'
                }}
                value={filters.minQuantity}
                onChange={(e) => setFilters(prev => ({ ...prev, minQuantity: e.target.value }))}
                placeholder="0"
              />
            </div>

            <div>
              <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                Max Quantity
              </label>
              <input
                type="number"
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ced4da',
                  borderRadius: '4px',
                  fontSize: '14px'
                }}
                value={filters.maxQuantity}
                onChange={(e) => setFilters(prev => ({ ...prev, maxQuantity: e.target.value }))}
                placeholder="∞"
              />
            </div>
          </div>

          <div style={{ display: 'flex', gap: '16px', marginTop: '16px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px', color: '#333' }}>
              <input
                type="checkbox"
                checked={filters.lowStock}
                onChange={(e) => setFilters(prev => ({ ...prev, lowStock: e.target.checked }))}
                style={{ width: '16px', height: '16px' }}
              />
              Show only low stock items
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px', color: '#333' }}>
              <input
                type="checkbox"
                checked={filters.expiringSoon}
                onChange={(e) => setFilters(prev => ({ ...prev, expiringSoon: e.target.checked }))}
                style={{ width: '16px', height: '16px' }}
              />
              Show only items expiring soon
            </label>
          </div>
        </div>
      )}

      {/* Results */}
      {loading ? (
        <div style={{ textAlign: 'center', padding: '32px 0', color: '#6c757d' }}>
          ⏳ Loading inventory...
        </div>
      ) : error ? (
        <div style={{ 
          color: '#dc3545', 
          fontWeight: 'bold',
          backgroundColor: '#f8d7da',
          padding: '16px',
          borderRadius: '4px',
          border: '1px solid #f5c6cb',
          textAlign: 'center'
        }}>
          ❌ {error}
        </div>
      ) : hasResults ? (
        <div>
          <div style={{ 
            display: 'flex', 
            justifyContent: 'space-between', 
            alignItems: 'center', 
            marginBottom: '16px' 
          }}>
            <h4 style={{ fontSize: '16px', fontWeight: 'bold', color: '#333' }}>
              Search Results ({total} total items)
            </h4>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', color: '#6c757d', fontSize: '14px' }}>
              <span>Page {page} of {totalPages}</span>
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setPage(1);
                }}
                style={{
                  padding: '4px 8px',
                  border: '1px solid #ced4da',
                  borderRadius: '4px',
                  fontSize: '14px'
                }}
              >
                <option value={10}>10 per page</option>
                <option value={20}>20 per page</option>
                <option value={50}>50 per page</option>
              </select>
            </div>
          </div>
          <div style={{ borderTop: '1px solid #dee2e6' }}>
            {results.map(item => (
              <div key={item.id} style={{ 
                padding: '16px', 
                borderBottom: '1px solid #dee2e6',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'flex-start'
              }}>
                <div style={{ flex: 1 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '8px' }}>
                    <h4 style={{ fontSize: '16px', fontWeight: 'bold', color: '#333' }}>{item.name}</h4>
                    {item.category && (
                      <span style={{ 
                        padding: '4px 8px', 
                        backgroundColor: '#d1ecf1', 
                        color: '#0c5460', 
                        fontSize: '12px', 
                        borderRadius: '9999px' 
                      }}>
                        {item.category}
                      </span>
                    )}
                    {item.quantity <= 50 && (
                      <span style={{ 
                        padding: '4px 8px', 
                        backgroundColor: '#f8d7da', 
                        color: '#dc3545', 
                        fontSize: '12px', 
                        borderRadius: '9999px',
                        display: 'flex',
                        alignItems: 'center',
                        gap: '4px'
                      }}>
                        ⚠️ Low Stock
                      </span>
                    )}
                  </div>
                  <div style={{ display: 'flex', gap: '24px', fontSize: '14px', color: '#6c757d' }}>
                    <span>ID: {item.id}</span>
                    <span style={{ 
                      fontWeight: 'bold', 
                      color: item.quantity <= 50 ? '#dc3545' : '#28a745' 
                    }}>
                      Quantity: {item.quantity}
                    </span>
                    {item.location && (
                      <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                        📍 {item.location}
                      </span>
                    )}
                    {item.manufacturer && <span>Manufacturer: {item.manufacturer}</span>}
                    {item.expiration && item.expiration !== "N/A" && (
                      <span>Expires: {item.expiration}</span>
                    )}
                  </div>
                </div>
                <div style={{ display: 'flex', gap: '8px', marginLeft: '16px' }}>
                  <button style={{
                    backgroundColor: '#1a5490',
                    color: 'white',
                    border: 'none',
                    padding: '8px 16px',
                    borderRadius: '4px',
                    cursor: 'pointer',
                    fontSize: '14px'
                  }}>
                    ✏️ Edit
                  </button>
                  <button 
                    onClick={() => handleDelete(item.id)}
                    style={{
                      backgroundColor: '#dc3545',
                      color: 'white',
                      border: 'none',
                      padding: '8px 16px',
                      borderRadius: '4px',
                      cursor: 'pointer',
                      fontSize: '14px'
                    }}
                  >
                    🗑️ Delete
                  </button>
                </div>
              </div>
            ))}
          </div>
          
          {/* Pagination */}
          <div style={{ 
            display: 'flex', 
            justifyContent: 'space-between', 
            alignItems: 'center', 
            padding: '16px 0',
            fontSize: '14px',
            color: '#6c757d'
          }}>
            <div>
              Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, total)} of {total} results
            </div>
            <div style={{ display: 'flex', gap: '8px' }}>
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page <= 1}
                style={{
                  backgroundColor: '#f8f9fa',
                  border: '1px solid #ced4da',
                  color: '#333',
                  padding: '8px 16px',
                  borderRadius: '4px',
                  cursor: page <= 1 ? 'not-allowed' : 'pointer',
                  opacity: page <= 1 ? 0.5 : 1
                }}
              >
                Previous
              </button>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                style={{
                  backgroundColor: '#f8f9fa',
                  border: '1px solid #ced4da',
                  color: '#333',
                  padding: '8px 16px',
                  borderRadius: '4px',
                  cursor: page >= totalPages ? 'not-allowed' : 'pointer',
                  opacity: page >= totalPages ? 0.5 : 1
                }}
              >
                Next
              </button>
            </div>
          </div>
        </div>
      ) : showEmpty ? (
        <div style={{ textAlign: 'center', padding: '32px 0' }}>
          {isSearching ? (
            <>
              <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: '#333', marginBottom: '8px' }}>No items found</h3>
              <p style={{ color: '#6c757d', marginBottom: '16px' }}>Try adjusting your search terms or filters</p>
              <button
                onClick={clearFilters}
                style={{
                  backgroundColor: '#1a5490',
                  color: 'white',
                  border: 'none',
                  padding: '8px 16px',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  fontSize: '14px'
                }}
              >
                Clear search
              </button>
            </>
          ) : (
            <>
              <h3 style={{ fontSize: '18px', fontWeight: 'bold', color: '#333', marginBottom: '8px' }}>Start searching</h3>
              <p style={{ color: '#6c757d' }}>Enter a search term, ID, or use quick filters to find inventory items</p>
            </>
          )}
        </div>
      ) : null}
    </div>
  );
};

export default HospitalInventorySearch;