import React, { useState } from "react";
import { createItem } from '../api/items';

export default function ItemForm({ onCreated }) {
    const [formData, setFormData] = useState({
        name: '',
        quantity: '',
        category: '',
        location: '',
        supplier: '',
        barcode: '',
        expirationDate: '',
        lotNumber: '',
        unitCost: '',
        reorderLevel: '',
        description: ''
    });
    
    const [loading, setLoading] = useState(false);
    const [errors, setErrors] = useState({});
    const [success, setSuccess] = useState('');
    const [showAdvanced, setShowAdvanced] = useState(false);

    // Medical supply categories
    const categories = [
        'Medications',
        'Surgical Instruments',
        'Disposable Supplies',
        'Medical Devices',
        'Laboratory Supplies',
        'PPE (Personal Protective Equipment)',
        'Implants',
        'Emergency Supplies',
        'Cleaning/Sanitation',
        'Other'
    ];

    const locations = [
        'Central Pharmacy',
        'OR Suite 1-5',
        'Emergency Department',
        'ICU',
        'Medical Supplies Storage',
        'Laboratory',
        'Sterile Processing',
        'Main Warehouse',
        'Floor 2 Supply Room',
        'Floor 3 Supply Room'
    ];

    // Comprehensive validation
    const validateForm = () => {
        const newErrors = {};
        
        // Required field validation
        if (!formData.name.trim()) {
            newErrors.name = 'Item name is required';
        } else if (formData.name.length > 200) {
            newErrors.name = 'Item name cannot exceed 200 characters';
        }
        
        // Quantity validation
        const qty = Number(formData.quantity);
        if (!formData.quantity || !Number.isFinite(qty) || qty < 0) {
            newErrors.quantity = 'Quantity must be a non-negative number';
        } else if (qty > 999999) {
            newErrors.quantity = 'Quantity cannot exceed 999,999';
        }
        
        // Category validation
        if (!formData.category) {
            newErrors.category = 'Category is required for inventory management';
        }
        
        // Location validation
        if (!formData.location) {
            newErrors.location = 'Storage location is required';
        }
        
        // Barcode validation 
        if (formData.barcode && !/^[0-9A-Z\-]+$/.test(formData.barcode)) {
            newErrors.barcode = 'Invalid barcode format';
        }
        
        // Cost validation 
        if (formData.unitCost && (isNaN(formData.unitCost) || Number(formData.unitCost) < 0)) {
            newErrors.unitCost = 'Unit cost must be a positive number';
        }
        
        // Reorder level validation
        const reorder = Number(formData.reorderLevel);
        if (formData.reorderLevel && (!Number.isFinite(reorder) || reorder < 0)) {
            newErrors.reorderLevel = 'Reorder level must be a non-negative number';
        }
        
        // Expiration date validation 
        if (formData.expirationDate) {
            const expDate = new Date(formData.expirationDate);
            const today = new Date();
            if (expDate < today) {
                newErrors.expirationDate = 'Warning: Expiration date is in the past';
            }
        }
        
        return newErrors;
    };

    const handleInputChange = (field, value) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        
        // Clear error for this field when user starts typing
        if (errors[field]) {
            setErrors(prev => ({ ...prev, [field]: '' }));
        }
    };

    async function handleSubmit(e) {
        e.preventDefault();
        
        const validationErrors = validateForm();
        if (Object.keys(validationErrors).length > 0) {
            setErrors(validationErrors);
            return;
        }
        
        setLoading(true);
        setErrors({});
        setSuccess('');

        try {
            // Prepare data for API
            const itemData = {
                name: formData.name.trim(),
                quantity: Number(formData.quantity),
                category: formData.category,
                location: formData.location,
                ...(formData.supplier && { supplier: formData.supplier.trim() }),
                ...(formData.barcode && { barcode: formData.barcode.trim() }),
                ...(formData.expirationDate && { expirationDate: formData.expirationDate }),
                ...(formData.lotNumber && { lotNumber: formData.lotNumber.trim() }),
                ...(formData.unitCost && { unitCost: Number(formData.unitCost) }),
                ...(formData.reorderLevel && { reorderLevel: Number(formData.reorderLevel) }),
                ...(formData.description && { description: formData.description.trim() })
            };
            
            const created = await createItem(itemData);
            
            setSuccess(`✓ Successfully added: ${created.name} (ID: ${created.id})`);
            
            // Reset form
            setFormData({
                name: '', quantity: '', category: '', location: '', supplier: '',
                barcode: '', expirationDate: '', lotNumber: '', unitCost: '',
                reorderLevel: '', description: ''
            });
            
            onCreated?.(created);
            
            // Clear success message after 5 seconds
            setTimeout(() => setSuccess(''), 5000);
            
        } catch (err) {
            console.error('Error creating item:', err);
            setErrors({ submit: err.message || 'Failed to create item. Please try again.' });
        } finally {
            setLoading(false);
        }
    }

    return (
        <div style={{ 
            backgroundColor: '#f8f9fa', 
            padding: '24px', 
            borderRadius: '8px', 
            border: '1px solid #e9ecef',
            marginBottom: '24px'
        }}>
            <h3 style={{ margin: '0 0 16px 0', color: '#1a5490' }}>
                📦 Add New Inventory Item
            </h3>
            
            <div onSubmit={handleSubmit}>
                {/* Basic Information Section */}
                <div style={{ 
                    display: 'grid', 
                    gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
                    gap: '16px',
                    marginBottom: '20px'
                }}>
                    <div>
                        <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                            Item Name *
                        </label>
                        <input
                            placeholder="e.g., Surgical Gloves, Aspirin 500mg"
                            value={formData.name}
                            onChange={(e) => handleInputChange('name', e.target.value)}
                            style={{
                                width: '100%',
                                padding: '8px 12px',
                                border: errors.name ? '2px solid #dc3545' : '1px solid #ced4da',
                                borderRadius: '4px',
                                fontSize: '14px'
                            }}
                            required
                            autoFocus
                        />
                        {errors.name && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.name}</span>}
                    </div>

                    <div>
                        <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                            Quantity *
                        </label>
                        <input
                            placeholder="0"
                            value={formData.quantity}
                            onChange={(e) => handleInputChange('quantity', e.target.value)}
                            type="number"
                            min="0"
                            step="1"
                            style={{
                                width: '100%',
                                padding: '8px 12px',
                                border: errors.quantity ? '2px solid #dc3545' : '1px solid #ced4da',
                                borderRadius: '4px',
                                fontSize: '14px'
                            }}
                            required
                        />
                        {errors.quantity && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.quantity}</span>}
                    </div>

                    <div>
                        <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                            Category *
                        </label>
                        <select
                            value={formData.category}
                            onChange={(e) => handleInputChange('category', e.target.value)}
                            style={{
                                width: '100%',
                                padding: '8px 12px',
                                border: errors.category ? '2px solid #dc3545' : '1px solid #ced4da',
                                borderRadius: '4px',
                                fontSize: '14px'
                            }}
                            required
                        >
                            <option value="">Select Category</option>
                            {categories.map(cat => (
                                <option key={cat} value={cat}>{cat}</option>
                            ))}
                        </select>
                        {errors.category && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.category}</span>}
                    </div>

                    <div>
                        <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                            Storage Location *
                        </label>
                        <select
                            value={formData.location}
                            onChange={(e) => handleInputChange('location', e.target.value)}
                            style={{
                                width: '100%',
                                padding: '8px 12px',
                                border: errors.location ? '2px solid #dc3545' : '1px solid #ced4da',
                                borderRadius: '4px',
                                fontSize: '14px'
                            }}
                            required
                        >
                            <option value="">Select Location</option>
                            {locations.map(loc => (
                                <option key={loc} value={loc}>{loc}</option>
                            ))}
                        </select>
                        {errors.location && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.location}</span>}
                    </div>
                </div>

                {/* Advanced Fields Toggle */}
                <button
                    type="button"
                    onClick={() => setShowAdvanced(!showAdvanced)}
                    style={{
                        background: 'none',
                        border: '1px solid #6c757d',
                        color: '#6c757d',
                        padding: '8px 16px',
                        borderRadius: '4px',
                        cursor: 'pointer',
                        marginBottom: '16px',
                        fontSize: '14px'
                    }}
                >
                    {showAdvanced ? '▼ Hide Advanced Options' : '▶ Show Advanced Options'}
                </button>

                {/* Advanced Fields */}
                {showAdvanced && (
                    <div style={{ 
                        display: 'grid', 
                        gridTemplateColumns: 'repeat(auto-fit, minmax(250px, 1fr))', 
                        gap: '16px',
                        marginBottom: '20px',
                        padding: '16px',
                        backgroundColor: '#ffffff',
                        borderRadius: '4px',
                        border: '1px solid #dee2e6'
                    }}>
                        <div>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Supplier/Vendor
                            </label>
                            <input
                                placeholder="e.g., Johnson & Johnson, Medline"
                                value={formData.supplier}
                                onChange={(e) => handleInputChange('supplier', e.target.value)}
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px'
                                }}
                            />
                        </div>

                        <div>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Barcode/SKU
                            </label>
                            <input
                                placeholder="Scan or enter barcode"
                                value={formData.barcode}
                                onChange={(e) => handleInputChange('barcode', e.target.value)}
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: errors.barcode ? '2px solid #dc3545' : '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px'
                                }}
                            />
                            {errors.barcode && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.barcode}</span>}
                        </div>

                        <div>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Expiration Date
                            </label>
                            <input
                                type="date"
                                value={formData.expirationDate}
                                onChange={(e) => handleInputChange('expirationDate', e.target.value)}
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: errors.expirationDate ? '2px solid #ffc107' : '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px'
                                }}
                            />
                            {errors.expirationDate && <span style={{ color: '#ffc107', fontSize: '12px' }}>{errors.expirationDate}</span>}
                        </div>

                        <div>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Lot Number
                            </label>
                            <input
                                placeholder="Manufacturing lot/batch number"
                                value={formData.lotNumber}
                                onChange={(e) => handleInputChange('lotNumber', e.target.value)}
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px'
                                }}
                            />
                        </div>

                        <div>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Unit Cost ($)
                            </label>
                            <input
                                placeholder="0.00"
                                value={formData.unitCost}
                                onChange={(e) => handleInputChange('unitCost', e.target.value)}
                                type="number"
                                min="0"
                                step="0.01"
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: errors.unitCost ? '2px solid #dc3545' : '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px'
                                }}
                            />
                            {errors.unitCost && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.unitCost}</span>}
                        </div>

                        <div>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Reorder Level
                            </label>
                            <input
                                placeholder="Minimum quantity before reorder"
                                value={formData.reorderLevel}
                                onChange={(e) => handleInputChange('reorderLevel', e.target.value)}
                                type="number"
                                min="0"
                                step="1"
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: errors.reorderLevel ? '2px solid #dc3545' : '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px'
                                }}
                            />
                            {errors.reorderLevel && <span style={{ color: '#dc3545', fontSize: '12px' }}>{errors.reorderLevel}</span>}
                        </div>

                        <div style={{ gridColumn: '1 / -1' }}>
                            <label style={{ display: 'block', marginBottom: '4px', fontWeight: 'bold', color: '#333' }}>
                                Description/Notes
                            </label>
                            <textarea
                                placeholder="Additional details, special handling instructions, etc."
                                value={formData.description}
                                onChange={(e) => handleInputChange('description', e.target.value)}
                                rows="3"
                                style={{
                                    width: '100%',
                                    padding: '8px 12px',
                                    border: '1px solid #ced4da',
                                    borderRadius: '4px',
                                    fontSize: '14px',
                                    resize: 'vertical'
                                }}
                            />
                        </div>
                    </div>
                )}

                {/* Submit Button and Status Messages */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
                    <button 
                        type="submit" 
                        disabled={loading}
                        onClick={handleSubmit}
                        style={{
                            backgroundColor: loading ? '#6c757d' : '#1a5490',
                            color: 'white',
                            border: 'none',
                            padding: '12px 24px',
                            borderRadius: '4px',
                            cursor: loading ? 'not-allowed' : 'pointer',
                            fontSize: '16px',
                            fontWeight: 'bold'
                        }}
                    >
                        {loading ? '⏳ Adding Item...' : '✓ Add to Inventory'}
                    </button>

                    {success && (
                        <div style={{ 
                            color: '#28a745', 
                            fontWeight: 'bold',
                            backgroundColor: '#d4edda',
                            padding: '8px 12px',
                            borderRadius: '4px',
                            border: '1px solid #c3e6cb'
                        }}>
                            {success}
                        </div>
                    )}

                    {errors.submit && (
                        <div style={{ 
                            color: '#dc3545', 
                            fontWeight: 'bold',
                            backgroundColor: '#f8d7da',
                            padding: '8px 12px',
                            borderRadius: '4px',
                            border: '1px solid #f5c6cb'
                        }}>
                            ❌ {errors.submit}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}