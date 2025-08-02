import './App.css';
import React, { useEffect, useState } from 'react';
import ItemLookup from './components/ItemLookup';
import ItemList from './components/ItemList';

function App() {
  const [apiData, setApiData] = useState(null);

  useEffect(() => {
    fetch('http://localhost:5200/health', {
      method: "GET",
      headers: {
        "Content-Type": "application/json"
      }
    })
      .then(response => {
        console.log('Response status', response.status);
        if(!response.ok) throw new Error(`HTTP ${response.status}`);
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

  return (
    <div className="App">
      <h1>Hospital Inventory Dashboard</h1>
      <ItemLookup />
      <ItemList />
      <p>API Response: { apiData ? apiData.status : 'Loading...'}</p>
      </div>
  );
  }

export default App;
