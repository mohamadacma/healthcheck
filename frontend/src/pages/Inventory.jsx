import { useState } from 'react';
import ItemForm from '../components/ItemForm';
import HospitalInventorySearch from '../components/search';
import ItemEditForm from '../components/ItemEditForm';

import { clearToken } from '../api/client';

export default function Inventory({ onLogout }) {
    const [refreshKey, setRefreshKey] = useState(0);
    const [editingItem, setEditingItem] = useState(null);

    const handleEditSuccess = () => {
        setEditingItem(null);
        setRefreshKey(k => k + 1);
    };

    return (
        <div style={{ maxWidth: 960, margin: '0 auto', padding: 24 }}>
          <header style={{ display:'flex', alignItems:'center', marginBottom:16 }}>
            <h1 style={{ margin: 0 }}>Hospital Inventory Dashboard</h1>
            <button style={{ marginLeft:'auto' }} onClick={() => { clearToken(); onLogout?.(); }}>
              Logout
            </button>
          </header>
    
          <ItemForm onCreated={() => setRefreshKey(k => k + 1)} />
          <HospitalInventorySearch
            refreshTrigger={refreshKey}
            onEditItem={setEditingItem}
            />

        {editingItem && (
            <div style={{
                position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
                background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center'
            }}>
                <div style={{ background: 'white', padding: 24, borderRadius: 8, maxWidth: 600 }}>
                <ItemEditForm
                    item={editingItem}
                    onSuccess={handleEditSuccess}
                    onCancel={() => setEditingItem(null)}
                    />
                </div>
        </div>
      )}
      </div>
    );
    }