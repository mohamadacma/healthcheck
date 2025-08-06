import './App.css';

import ItemEditForm from './components/ItemEditForm';
import ItemForm from './components/ItemForm';
import ItemList from './components/ItemList';
import Inventory from './pages/Inventory';
import ItemLookup from './components/ItemLookup';
import Login from './pages/Login';
import { getToken } from './api/client';


import React, { useState, useEffect } from "react";

function App() {
  const [authorized, setAuthorized] = useState(!!getToken());
  const [apiData, setApiData] = useState(null);
  const [refreshKey, setRefreshKey] = useState(0);

  useEffect(() => {
    fetch('http://localhost:5200/health', {
      method: "GET",
      headers: {
        "Content-Type": "application/json"
      }
    })
      .then(response => {
        console.log('Response status', response.status);
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        return response.json();
      })
      .then(data => {
        console.log('API Data:', data);
        setApiData(data);
      })
      .catch(error => {
        console.error('Fetch error:', error);
      });
  }, []);
 
  return authorized ? (
    <div className="App">
      <h1>Hospital Inventory Dashboard</h1>
      <ItemForm onCreated={() => setRefreshKey(k => k + 1)} />
      <ItemList refreshKey={refreshKey} />
      <ItemLookup />
      <ItemEditForm
        item={{ id: 5, name: 'Pads', quantity: 15 }}
        onUpdated={() => setRefreshKey(k => k + 1)}
      />
      <p>API Response: {apiData ? apiData.status : 'Loading...'}</p>
    </div>
  ) : (
    <Login onSuccess={() => setAuthorized(true)} />
  );
}
export default App;