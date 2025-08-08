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
        setLoading(true);
        setError(''); 
        setItem(null);
        setSuccess('');
        try {
            const data = await getItem(id);
            setItem(data);
            setSuccess(`Item ID ${id} loaded successfully ✅`);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    const handleKeyPress = (e) => {
        if (e.key == 'Enter') {
            onFetch();
        }
    };

    return (
        <div style={{ maxWidth: '600px', margin: '0 auto', padding: '20px', fontFamily: 'Arial, sans-serif' }}>
            <div style={{ textAlign: 'center', marginBottom: '30px' }}>
                <h1 style={{ color: '#2563eb', marginBottom: '10px' }}>🏥 Hospital Inventory</h1>
                <p style={{ color: '#666', margin: '0' }}>Search for medical supplies by ID</p>
            </div>

            <div style={{ marginBottom: '20px' }}>
                <div style={{ display: 'flex', gap: '10px' }}>
                    <input 
                        value={id} 
                        onChange={e => setId(e.target.value)}
                        onKeyPress={handleKeyPress}
                        placeholder="Enter Item ID (e.g., MED001)" 
                        style={{
                            flex: '1',
                            padding: '12px',
                            border: '2px solid #e5e7eb',
                            borderRadius: '8px',
                            fontSize: '16px',
                            outline: 'none'
                        }}
                        onFocus={(e) => e.target.style.borderColor = '#2563eb'}
                        onBlur={(e) => e.target.style.borderColor = '#e5e7eb'}
                        disabled={loading}
                    />
                    <button 
                        onClick={onFetch}
                        disabled={loading || !id.trim()}
                        style={{
                            padding: '12px 24px',
                            backgroundColor: loading || !id.trim() ? '#9ca3af' : '#2563eb',
                            color: 'white',
                            border: 'none',
                            borderRadius: '8px',
                            fontSize: '16px',
                            cursor: loading || !id.trim() ? 'not-allowed' : 'pointer',
                            minWidth: '100px'
                        }}
                    >
                        {loading ? '🔄 Loading...' : '🔍 Search'}
                    </button>
                </div>
            </div>

            {loading && (
                <div style={{
                    padding: '15px',
                    backgroundColor: '#dbeafe',
                    border: '1px solid #93c5fd',
                    borderRadius: '8px',
                    marginBottom: '20px',
                    color: '#1e40af'
                }}>
                    ⏳ Searching inventory database...
                </div>
            )}

            {success && (
                <div style={{
                    padding: '15px',
                    backgroundColor: '#dcfce7',
                    border: '1px solid #86efac',
                    borderRadius: '8px',
                    marginBottom: '20px',
                    color: '#166534'
                }}>
                    {success}
                </div>
            )}

            {error && (
                <div style={{
                    padding: '15px',
                    backgroundColor: '#fef2f2',
                    border: '1px solid #fca5a5',
                    borderRadius: '8px',
                    marginBottom: '20px',
                    color: '#dc2626'
                }}>
                    ❌ Error: {error}
                </div>
            )}

            {item && (
                <div style={{
                    border: '2px solid #e5e7eb',
                    borderRadius: '12px',
                    overflow: 'hidden'
                }}>
                    <div style={{
                        backgroundColor: '#2563eb',
                        color: 'white',
                        padding: '15px',
                        fontSize: '18px',
                        fontWeight: 'bold'
                    }}>
                        📦 Item Details
                    </div>
                    
                    <div style={{ padding: '20px', backgroundColor: '#f9fafb' }}>
                        <div style={{
                            display: 'grid',
                            gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))',
                            gap: '15px'
                        }}>
                            <div>
                                <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 'bold', marginBottom: '5px' }}>
                                    ITEM ID
                                </div>
                                <div style={{ fontSize: '18px', fontWeight: 'bold', color: '#111827' }}>
                                    {item.id}
                                </div>
                            </div>
                            
                            <div>
                                <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 'bold', marginBottom: '5px' }}>
                                    ITEM NAME
                                </div>
                                <div style={{ fontSize: '18px', fontWeight: 'bold', color: '#111827' }}>
                                    {item.name}
                                </div>
                            </div>
                            
                            <div>
                                <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 'bold', marginBottom: '5px' }}>
                                    CATEGORY
                                </div>
                                <div style={{
                                    display: 'inline-block',
                                    padding: '4px 12px',
                                    backgroundColor: '#dbeafe',
                                    color: '#1e40af',
                                    borderRadius: '20px',
                                    fontSize: '14px',
                                    fontWeight: 'bold'
                                }}>
                                    {item.category}
                                </div>
                            </div>
                            
                            <div>
                                <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 'bold', marginBottom: '5px' }}>
                                    QUANTITY
                                </div>
                                <div style={{ fontSize: '24px', fontWeight: 'bold', color: '#059669' }}>
                                    {item.quantity.toLocaleString()}
                                </div>
                            </div>
                            
                            <div>
                                <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 'bold', marginBottom: '5px' }}>
                                    LOCATION
                                </div>
                                <div style={{ fontSize: '16px', color: '#111827' }}>
                                    {item.location}
                                </div>
                            </div>
                            
                            <div>
                                <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 'bold', marginBottom: '5px' }}>
                                    SUPPLIER
                                </div>
                                <div style={{ fontSize: '16px', color: '#111827' }}>
                                    {item.supplier}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}