import React, { useState, useEffect } from 'react';
import { getItem } from '../api/items';
import { listItems } from '../api/items';
import { deleteItem } from '../api/items';
import { Search, Filter, Package, AlertTriangle, Calendar, MapPin, Edit, Trash2 } from 'lucide-react';

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
    <div className="max-w-7xl mx-auto p-6 bg-gray-50 min-h-screen">
      {/* Header */}
      <div className="bg-white rounded-lg shadow-sm p-6 mb-6">
        <div className="flex items-center gap-3 mb-4">
          <Package className="text-blue-600" size={32} />
          <h1 className="text-3xl font-bold text-gray-900">Hospital Inventory Search</h1>
        </div>
        
        {/* Main Search Bar */}
        <div className="flex gap-3 mb-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" size={20} />
            <input
              type="text"
              placeholder="Search by item name or enter ID for specific item..."
              className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
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
            className="flex items-center gap-2 px-4 py-3 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
          >
            <Filter size={20} />
            Advanced
          </button>
          <button
            onClick={clearFilters}
            className="px-4 py-3 bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors"
            disabled={!isSearching}
          >
            Clear All
          </button>
        </div>

        {/* Quick Action Buttons */}
        <div className="flex flex-wrap gap-2 mb-4">
          <button
            onClick={() => handleQuickSearch('lowStock')}
            className="flex items-center gap-2 px-4 py-2 bg-red-100 text-red-700 rounded-full hover:bg-red-200 transition-colors"
          >
            <AlertTriangle size={16} />
            Low Stock Items
          </button>
          <button
            onClick={() => handleQuickSearch('expiring')}
            className="flex items-center gap-2 px-4 py-2 bg-yellow-100 text-yellow-700 rounded-full hover:bg-yellow-200 transition-colors"
          >
            <Calendar size={16} />
            Expiring Soon
          </button>
          <button
            onClick={() => handleQuickSearch('medications')}
            className="flex items-center gap-2 px-4 py-2 bg-blue-100 text-blue-700 rounded-full hover:bg-blue-200 transition-colors"
          >
            <Package size={16} />
            Medications
          </button>
        </div>

        {/* Advanced Filters */}
        {showAdvanced && (
          <div className="border-t pt-4">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Category</label>
                <select
                  className="w-full p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
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
                <label className="block text-sm font-medium text-gray-700 mb-2">Location</label>
                <select
                  className="w-full p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
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
                <label className="block text-sm font-medium text-gray-700 mb-2">Min Quantity</label>
                <input
                  type="number"
                  className="w-full p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                  value={filters.minQuantity}
                  onChange={(e) => setFilters(prev => ({ ...prev, minQuantity: e.target.value }))}
                  placeholder="0"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">Max Quantity</label>
                <input
                  type="number"
                  className="w-full p-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
                  value={filters.maxQuantity}
                  onChange={(e) => setFilters(prev => ({ ...prev, maxQuantity: e.target.value }))}
                  placeholder="∞"
                />
              </div>
            </div>

            <div className="flex gap-4 mt-4">
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={filters.lowStock}
                  onChange={(e) => setFilters(prev => ({ ...prev, lowStock: e.target.checked }))}
                  className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700">Show only low stock items</span>
              </label>
              <label className="flex items-center gap-2">
                <input
                  type="checkbox"
                  checked={filters.expiringSoon}
                  onChange={(e) => setFilters(prev => ({ ...prev, expiringSoon: e.target.checked }))}
                  className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm text-gray-700">Show only items expiring soon</span>
              </label>
            </div>
          </div>
        )}
      </div>

      {/* Results */}
      {loading ? (
        <div className="flex justify-center items-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <span className="ml-3 text-gray-600">Loading inventory...</span>
        </div>
      ) : error ? (
        <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
          <AlertTriangle className="text-red-500 mx-auto mb-2" size={24} />
          <p className="text-red-700 font-medium">Error: {error}</p>
        </div>
      ) : hasResults ? (
        <div className="bg-white rounded-lg shadow-sm">
          <div className="p-4 border-b border-gray-200 flex justify-between items-center">
            <h3 className="text-lg font-semibold text-gray-900">
              Search Results ({total} total items)
            </h3>
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <span>Page {page} of {totalPages}</span>
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setPage(1);
                }}
                className="ml-2 border border-gray-300 rounded px-2 py-1"
              >
                <option value={10}>10 per page</option>
                <option value={20}>20 per page</option>
                <option value={50}>50 per page</option>
              </select>
            </div>
          </div>
          <div className="divide-y divide-gray-200">
            {results.map(item => (
              <div key={item.id} className="p-4 hover:bg-gray-50 transition-colors">
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <h4 className="text-lg font-medium text-gray-900">{item.name}</h4>
                      {item.category && (
                        <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded-full">
                          {item.category}
                        </span>
                      )}
                      {item.quantity <= 50 && (
                        <span className="px-2 py-1 bg-red-100 text-red-800 text-xs rounded-full flex items-center gap-1">
                          <AlertTriangle size={12} />
                          Low Stock
                        </span>
                      )}
                    </div>
                    <div className="flex items-center gap-6 text-sm text-gray-600">
                      <span>ID: {item.id}</span>
                      <span className={`font-medium ${item.quantity <= 50 ? 'text-red-600' : 'text-green-600'}`}>
                        Quantity: {item.quantity}
                      </span>
                      {item.location && (
                        <span className="flex items-center gap-1">
                          <MapPin size={14} />
                          {item.location}
                        </span>
                      )}
                      {item.manufacturer && <span>Manufacturer: {item.manufacturer}</span>}
                      {item.expiration && item.expiration !== "N/A" && (
                        <span>Expires: {item.expiration}</span>
                      )}
                    </div>
                  </div>
                  <div className="flex gap-2 ml-4">
                    <button className="flex items-center gap-1 px-3 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 transition-colors">
                      <Edit size={14} />
                      Edit
                    </button>
                    <button 
                      onClick={() => handleDelete(item.id)}
                      className="flex items-center gap-1 px-3 py-1 bg-red-600 text-white text-sm rounded hover:bg-red-700 transition-colors"
                    >
                      <Trash2 size={14} />
                      Delete
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
          
          {/* Pagination */}
          <div className="p-4 border-t border-gray-200 flex justify-between items-center">
            <div className="text-sm text-gray-600">
              Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, total)} of {total} results
            </div>
            <div className="flex gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Previous
              </button>
              <button
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1 border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      ) : showEmpty ? (
        <div className="bg-white rounded-lg shadow-sm p-12 text-center">
          {isSearching ? (
            <>
              <Package size={48} className="text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No items found</h3>
              <p className="text-gray-600 mb-4">Try adjusting your search terms or filters</p>
              <button
                onClick={clearFilters}
                className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
              >
                Clear search
              </button>
            </>
          ) : (
            <>
              <Search size={48} className="text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">Start searching</h3>
              <p className="text-gray-600">Enter a search term, ID, or use quick filters to find inventory items</p>
            </>
          )}
        </div>
      ) : null}
    </div>
  );
};

export default HospitalInventorySearch;