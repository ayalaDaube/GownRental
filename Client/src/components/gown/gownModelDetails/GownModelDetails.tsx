import React, { useState } from 'react';
import './GownModelDetails.css';
import { ModelDetails } from '../gownModel/GownModel';
import { useDispatch, useSelector } from 'react-redux';
import { addToCart, deleteGownThunk, updateGownThunk, fetchGroupsGowns } from '../../../redux/Gowns/GownSlice';
import { FaShoppingCart, FaTrashAlt, FaEdit } from 'react-icons/fa';

interface GownModelDetailsProps {
    details: ModelDetails[];
    gownInfo: { id: number, name: string, color: string, description: string, image?: string };
}

function GownModelDetails({ details, gownInfo }: GownModelDetailsProps) {
    const dispatch = useDispatch();
    const cart = useSelector((state: any) => state.gowns.Cart);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [editForm, setEditForm] = useState<Partial<ModelDetails>>({});

    const handleEditClick = (detail: ModelDetails) => {
        setEditingId(detail.gownId);
        setEditForm({ size: detail.size, price: detail.price, isAvailable: detail.isAvailable });
    };

    const handleDelete = (detail: ModelDetails) => {
        if (!window.confirm(`Delete size ${detail.size} (gown #${detail.gownId})?`)) return;
        dispatch(deleteGownThunk(detail.gownId) as any).then(() => dispatch(fetchGroupsGowns() as any));
    };

    const handleSave = (detail: ModelDetails) => {
        const payload = {
            id: detail.gownId,
            name: gownInfo.name,
            color: gownInfo.color,
            description: gownInfo.description,
            imageUrl: gownInfo.image || null,
            modelId: detail.modelId,
            size: Number(editForm.size),
            price: Number(editForm.price),
            isAvailable: editForm.isAvailable
        };
        dispatch(updateGownThunk(payload as any) as any)
        .then(() => {
            dispatch(fetchGroupsGowns() as any);
        })
        setEditingId(null);
    };

    return (
        <div className="gown-model-details">
            {details.map((detail) => {
                const inCart = cart.some((item: any) => item.gownId === detail.gownId);
                const isEditing = editingId === detail.gownId;
                return (
                    <div key={detail.gownId} className="gown-detail-card">
                        <div className="gown-detail-field"><span>Gown ID</span><strong>{detail.gownId}</strong></div>
                        {isEditing ? (
                            <>
                                <input type="number" value={editForm.size} onChange={e => setEditForm({...editForm, size: Number(e.target.value)})} placeholder="Size" className="gown-detail-edit-input" />
                                <input type="number" value={editForm.price} onChange={e => setEditForm({...editForm, price: Number(e.target.value)})} placeholder="Price" className="gown-detail-edit-input" />
                                <label className="gown-detail-checkbox">
                                    <input type="checkbox" checked={editForm.isAvailable} onChange={e => setEditForm({...editForm, isAvailable: e.target.checked})} />
                                    &nbsp;Available
                                </label>
                            </>
                        ) : (
                            <>
                                <div className="gown-detail-field"><span>Size</span><strong>{detail.size}</strong></div>
                                <div className="gown-detail-field"><span>Price</span><strong>{detail.price}₪</strong></div>
                                <div className="gown-detail-field"><span>Available</span><strong>{detail.isAvailable ? 'Yes' : 'No'}</strong></div>
                            </>
                        )}
                        <div className="gown-detail-actions">
                            {isEditing ? (
                                <>
                                    <button className="gown-update-submit-btn" onClick={() => handleSave(detail)}>Save</button>
                                    <button className="gown-update-cancel-btn" onClick={() => setEditingId(null)}>Cancel</button>
                                </>
                            ) : (
                                <button className="gown-update-btn" onClick={() => handleEditClick(detail)}><FaEdit /></button>
                            )}
                            <button className="gown-delete-btn" onClick={() => handleDelete(detail)}><FaTrashAlt /></button>
                            <button
                                className={`gown-cart-btn ${inCart ? 'gown-cart-btn--active' : ''} ${!detail.isAvailable ? 'gown-cart-btn--disabled' : ''}`}
                                title={!detail.isAvailable ? 'Not available' : inCart ? 'Remove from cart' : 'Add to cart'}
                                onClick={() => dispatch(addToCart({ ...detail, ...gownInfo }))}
                                disabled={!detail.isAvailable}
                            >
                                <FaShoppingCart />
                            </button>
                        </div>
                    </div>
                );
            })}
        </div>
    );
}

export default GownModelDetails;
