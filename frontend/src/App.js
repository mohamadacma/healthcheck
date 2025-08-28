import './App.css';
import React, { useState, useEffect } from "react";

import Inventory from './pages/Inventory';
import Login from './pages/Login';
import { getToken } from './api/client';

// Build-time flag to skip auth for a public demo
const DEMO = process.env.REACT_APP_DEMO === 'true';

function App() {
  const [authorized, setAuthorized] = useState(!!getToken());

  useEffect(() => {
    const onUnauthorized = () => setAuthorized(false);
    window.addEventListener('app:unauthorized', onUnauthorized);
    return () => window.removeEventListener('app:unauthorized', onUnauthorized);
  }, []);

  // In demo mode, show the app immediately (read-only)
  if (DEMO) {
    return <Inventory onLogout={() => { /* no-op in demo */ }} />;
  }

  return authorized
    ? <Inventory onLogout={() => setAuthorized(false)} />
    : <Login onSuccess={() => setAuthorized(true)} />;
}

export default App;
