import React, { useState } from 'react';
import './GownModelDetails.css';
import { ModelDetails } from '../gownModel/GownModel';
import { useDispatch, useSelector } from 'react-redux';
import { addGownThunk, addToCart, deleteGownThunk, updateGownSizeThunk, fetchGroupsGowns } from '../../../redux/Gowns/GownSlice';
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
    const [newSize, setNewSize] = useState({ size: '', price: '' });

    const handleEditClick = (detail: ModelDetails) => {
        setEditingId(detail.gownId);
        setEditForm({ size: detail.size, price: detail.price, isAvailable: detail.isAvailable });
    };

    const handleDelete = (detail: ModelDetails) => {
        if (!window.confirm(`Delete size ${detail.size} (gown #${detail.gownId})?`)) return;
        dispatch(deleteGownThunk(detail.gownId) as any).then(() => dispatch(fetchGroupsGowns() as any));
    };

    const handleSave = async (detail: ModelDetails) => {
        const result = await dispatch(updateGownSizeThunk({
            gownId: detail.gownId,
            changes: {
                size: Number(editForm.size),
                price: Number(editForm.price),
                isAvailable: editForm.isAvailable ?? true,
            },
        }) as any);
        dispatch(fetchGroupsGowns() as any);
        if (updateGownSizeThunk.fulfilled.match(result)) {
            setEditingId(null);
        }
    };

    const handleAddSize = async (e: React.FormEvent) => {
        e.preventDefault();
        const result = await dispatch(addGownThunk({
            modelId: gownInfo.id,
            size: Number(newSize.size),
            price: Number(newSize.price),
        }) as any);
        if (addGownThunk.fulfilled.match(result)) {
            setNewSize({ size: '', price: '' });
            dispatch(fetchGroupsGowns() as any);
        }
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
            <form className="gown-add-size" onSubmit={handleAddSize}>
                <span className="gown-add-size-label">Add a size</span>
                <input type="number" value={newSize.size} onChange={e => setNewSize({ ...newSize, size: e.target.value })} placeholder="Size" min={1} max={100} required className="gown-detail-edit-input" />
                <input type="number" value={newSize.price} onChange={e => setNewSize({ ...newSize, price: e.target.value })} placeholder="Price" min={1} max={100000} step="any" required className="gown-detail-edit-input" />
                <button type="submit" className="gown-update-submit-btn">Add</button>
            </form>
        </div>
    );
}

export default GownModelDetails;
