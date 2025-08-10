import './App.css';
import React, { useState, useEffect } from "react";


import Inventory from './pages/Inventory';
import Login from './pages/Login';
import { getToken } from './api/client';

function App() {
  const [authorized, setAuthorized] = useState(!!getToken());


  useEffect(() => {
    const onUnauthorized = () => setAuthorized(false);
    window.addEventListener('app:unauthorized', onUnauthorized);
    return () => window.removeEventListener('app:unauthorized', onUnauthorized);
  }, []);
 
  return authorized 
  ? <Inventory onLogout={() => setAuthorized(false)} />
  : <Login onSuccess={() => setAuthorized(true)} />;
}

     
export default App;