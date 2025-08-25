import { useState } from 'react';
import ItemForm from '../components/ItemForm';
import HospitalInventorySearch from '../components/search';
import ItemEditForm from '../components/ItemEditForm';
import ChatPanel from '../components/ChatPanel';
import { clearToken } from '../api/client';

export default function Inventory({ onLogout }) {
    const [refreshKey, setRefreshKey] = useState(0);
    const [editingItem, setEditingItem] = useState(null);
    const [showChat, setShowChat] = useState(false);

    const handleEditSuccess = () => {
        setEditingItem(null);
        setRefreshKey(k => k + 1);
    };

    const handleLogout = () => {
        clearToken();
        onLogout?.();
    };

    return (
        <div className="inventory-container">
            {/* Header */}
            <header className="dashboard-header">
                <div className="header-left">
                    <div className="header-icon">🏥</div>
                    <div className="header-content">
                        <h1 className="dashboard-title">Hospital Inventory Dashboard</h1>
                        <div className="status-indicator">
                            <div className="status-dot"></div>
                            System Online
                        </div>
                    </div>
                </div>
                <div className="header-actions">
                    <button 
                        className={`chat-toggle-btn ${showChat ? 'active' : ''}`}
                        onClick={() => setShowChat(v => !v)}
                    >
                        {showChat ? (
                            <>🔽 Hide Chat</>
                        ) : (
                            <>💬 Open Chat</>
                        )}
                    </button>
                    <button className="logout-btn" onClick={handleLogout}>
                        🚪 Logout
                    </button>
                </div>
            </header>

            {/* Main Content */}
            <div className="main-content">
                <div className="form-section">
                    <ItemForm onCreated={() => setRefreshKey(k => k + 1)} />
                </div>

                <div className="search-section">
                    <HospitalInventorySearch
                        refreshTrigger={refreshKey}
                        onEditItem={setEditingItem}
                    />
                </div>
            </div>

            {/* Edit Modal */}
            {editingItem && (
                <div className="modal-overlay" onClick={(e) => {
                    if (e.target === e.currentTarget) setEditingItem(null);
                }}>
                    <div className="modal-content">
                        <ItemEditForm
                            item={editingItem}
                            onUpdated={handleEditSuccess}
                            onCancel={() => setEditingItem(null)}
                        />
                    </div>
                </div>
            )}

            {/* Floating Chat Widget */}
            {showChat && (
                <div className="chat-widget">
                    <ChatPanel onClose={() => setShowChat(false)} />
                </div>
            )}

            <style jsx>{`
                .inventory-container {
                    min-height: 100vh;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                }

                .dashboard-header {
                    background: linear-gradient(145deg, #ffffff 0%, #f8fafc 100%);
                    border: 1px solid #e2e8f0;
                    border-radius: 20px;
                    box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1), 0 4px 12px rgba(0, 0, 0, 0.05);
                    margin: 24px;
                    padding: 24px 32px;
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    backdrop-filter: blur(10px);
                }

                .header-left {
                    display: flex;
                    align-items: center;
                    gap: 16px;
                }

                .header-icon {
                    width: 52px;
                    height: 52px;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    border-radius: 14px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    font-size: 26px;
                    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.3);
                }

                .header-content {
                    display: flex;
                    flex-direction: column;
                    gap: 4px;
                }

                .dashboard-title {
                    margin: 0;
                    font-size: 28px;
                    font-weight: 700;
                    color: #1f2937;
                    letter-spacing: -0.025em;
                    background: linear-gradient(135deg, #1f2937 0%, #374151 100%);
                    -webkit-background-clip: text;
                    -webkit-text-fill-color: transparent;
                    background-clip: text;
                }

                .status-indicator {
                    display: flex;
                    align-items: center;
                    gap: 6px;
                    font-size: 13px;
                    color: #16a34a;
                    font-weight: 500;
                }

                .status-dot {
                    width: 8px;
                    height: 8px;
                    background: #16a34a;
                    border-radius: 50%;
                    animation: pulse 2s infinite;
                    box-shadow: 0 0 0 2px rgba(22, 163, 74, 0.2);
                }

                .header-actions {
                    display: flex;
                    gap: 12px;
                    align-items: center;
                }

                .chat-toggle-btn, .logout-btn {
                    padding: 12px 20px;
                    border: none;
                    border-radius: 12px;
                    font-size: 14px;
                    font-weight: 600;
                    cursor: pointer;
                    transition: all 0.2s ease;
                    display: flex;
                    align-items: center;
                    gap: 8px;
                }

                .chat-toggle-btn {
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
                }

                .chat-toggle-btn:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 6px 16px rgba(102, 126, 234, 0.5);
                }

                .chat-toggle-btn.active {
                    background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
                    box-shadow: 0 4px 12px rgba(239, 68, 68, 0.4);
                }

                .chat-toggle-btn.active:hover {
                    box-shadow: 0 6px 16px rgba(239, 68, 68, 0.5);
                }

                .logout-btn {
                    background: white;
                    color: #6b7280;
                    border: 2px solid #e5e7eb;
                    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
                }

                .logout-btn:hover {
                    background: #f9fafb;
                    border-color: #d1d5db;
                    transform: translateY(-1px);
                    color: #374151;
                }

                .main-content {
                    max-width: 1200px;
                    margin: 0 auto;
                    padding: 0 24px 24px;
                    display: flex;
                    flex-direction: column;
                    gap: 32px;
                }

                .form-section, .search-section {
                    animation: slideIn 0.6s ease-out forwards;
                }

                .search-section {
                    animation-delay: 0.2s;
                }

                .modal-overlay {
                    position: fixed;
                    top: 0;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    background: rgba(0, 0, 0, 0.6);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 1000;
                    padding: 24px;
                    backdrop-filter: blur(4px);
                    animation: fadeIn 0.3s ease-out;
                }

                .modal-content {
                    background: linear-gradient(145deg, #ffffff 0%, #f8fafc 100%);
                    border-radius: 20px;
                    box-shadow: 0 25px 50px rgba(0, 0, 0, 0.25);
                    max-width: 600px;
                    width: 100%;
                    max-height: 90vh;
                    overflow-y: auto;
                    animation: slideUp 0.3s ease-out;
                    border: 1px solid #e2e8f0;
                }

                .chat-widget {
                    position: fixed;
                    right: 24px;
                    bottom: 24px;
                    z-index: 30;
                    animation: slideInUp 0.4s ease-out;
                    filter: drop-shadow(0 25px 50px rgba(0, 0, 0, 0.15));
                }

                /* Animations */
                @keyframes pulse {
                    0%, 100% {
                        opacity: 1;
                        transform: scale(1);
                    }
                    50% {
                        opacity: 0.7;
                        transform: scale(1.05);
                    }
                }

                @keyframes slideIn {
                    from {
                        opacity: 0;
                        transform: translateY(30px);
                    }
                    to {
                        opacity: 1;
                        transform: translateY(0);
                    }
                }

                @keyframes fadeIn {
                    from {
                        opacity: 0;
                    }
                    to {
                        opacity: 1;
                    }
                }

                @keyframes slideUp {
                    from {
                        opacity: 0;
                        transform: translateY(50px) scale(0.9);
                    }
                    to {
                        opacity: 1;
                        transform: translateY(0) scale(1);
                    }
                }

                @keyframes slideInUp {
                    from {
                        opacity: 0;
                        transform: translateY(100px);
                    }
                    to {
                        opacity: 1;
                        transform: translateY(0);
                    }
                }

                /* Responsive Design */
                @media (max-width: 768px) {
                    .dashboard-header {
                        flex-direction: column;
                        gap: 20px;
                        margin: 16px;
                        padding: 20px;
                        text-align: center;
                    }

                    .header-left {
                        flex-direction: column;
                        gap: 12px;
                    }

                    .header-actions {
                        width: 100%;
                        justify-content: center;
                        flex-wrap: wrap;
                    }

                    .chat-toggle-btn, .logout-btn {
                        flex: 1;
                        min-width: 140px;
                        justify-content: center;
                    }

                    .main-content {
                        padding: 0 16px 16px;
                        gap: 24px;
                    }

                    .modal-overlay {
                        padding: 16px;
                    }

                    .modal-content {
                        max-height: 95vh;
                    }

                    .chat-widget {
                        right: 16px;
                        bottom: 16px;
                        left: 16px;
                        position: fixed;
                    }

                    .dashboard-title {
                        font-size: 24px;
                    }
                }

                /* Dark mode compatibility */
                @media (prefers-color-scheme: dark) {
                    .inventory-container {
                        background: linear-gradient(135deg, #1e1b4b 0%, #581c87 100%);
                    }
                }

                /* Print styles */
                @media print {
                    .header-actions, .chat-widget, .modal-overlay {
                        display: none !important;
                    }
                    
                    .inventory-container {
                        background: white !important;
                    }
                }
            `}</style>
        </div>
    );
}