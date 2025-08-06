import { useState } from 'react';
import ItemForm from '../components/ItemForm';
import ItemList from '../components/ItemList';
import ItemLookup from '../components/ItemLookup';
import { clearToken } from '../api/client';

export default function Inventory({ onLogout }) {
    const [refreshKey, setRefreshKey] = useState(0);

    return (
        <div style={{ maxWidth: 960, margin: '0 auto', padding: 24 }}>
          <header style={{ display:'flex', alignItems:'center', marginBottom:16 }}>
            <h1 style={{ margin: 0 }}>Hospital Inventory Dashboard</h1>
            <button style={{ marginLeft:'auto' }} onClick={() => { clearToken(); onLogout?.(); }}>
              Logout
            </button>
          </header>
    
          <ItemForm onCreated={() => setRefreshKey(k => k + 1)} />
          <ItemList refreshKey={refreshKey} />
          <ItemLookup />
        </div>
      );
    }