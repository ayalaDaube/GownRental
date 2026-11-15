import React, { useState } from 'react';
import './GownModelDetails.css';
import { CartItem, GownSize } from '../../../redux/Gowns/GownTypes';
import { useAppDispatch, useAppSelector } from '../../../store/hooks';
import { addGownThunk, addToCart, deleteGownThunk, updateGownSizeThunk, fetchGroupsGowns } from '../../../redux/Gowns/GownSlice';
import { FaShoppingCart, FaTrashAlt, FaEdit } from 'react-icons/fa';

interface GownModelDetailsProps {
    details: GownSize[];
    gownInfo: { id: number, name: string, color: string, description?: string | null, image?: string | null };
}

const toCartItem = (detail: GownSize, gownInfo: GownModelDetailsProps['gownInfo']): CartItem => ({
    ...detail,
    name: gownInfo.name,
    color: gownInfo.color,
    description: gownInfo.description,
    image: gownInfo.image,
});

function GownModelDetails({ details, gownInfo }: GownModelDetailsProps) {
    const dispatch = useAppDispatch();
    const cart = useAppSelector((state) => state.gowns.Cart);
    const [editingId, setEditingId] = useState<number | null>(null);
    const [editForm, setEditForm] = useState<Partial<GownSize>>({});
    const [newSize, setNewSize] = useState({ size: '', price: '' });

    const handleEditClick = (detail: GownSize) => {
        setEditingId(detail.gownId);
        setEditForm({ size: detail.size, price: detail.price, isActive: detail.isActive });
    };

    const handleDelete = (detail: GownSize) => {
        if (!window.confirm(`Delete size ${detail.size} (gown #${detail.gownId})?`)) return;
        dispatch(deleteGownThunk(detail.gownId)).then(() => dispatch(fetchGroupsGowns()));
    };

    const handleSave = async (detail: GownSize) => {
        const result = await dispatch(updateGownSizeThunk({
            gownId: detail.gownId,
            changes: {
                size: Number(editForm.size),
                price: Number(editForm.price),
                isActive: editForm.isActive ?? true,
            },
        }));
        dispatch(fetchGroupsGowns());
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
        }));
        if (addGownThunk.fulfilled.match(result)) {
            setNewSize({ size: '', price: '' });
            dispatch(fetchGroupsGowns());
        }
    };

    return (
        <div className="gown-model-details">
            {details.map((detail) => {
                const inCart = cart.some(item => item.gownId === detail.gownId);
                const isEditing = editingId === detail.gownId;
                return (
                    <div key={detail.gownId} className="gown-detail-card">
                        <div className="gown-detail-field"><span>Gown ID</span><strong>{detail.gownId}</strong></div>
                        {isEditing ? (
                            <>
                                <input type="number" value={editForm.size} onChange={e => setEditForm({...editForm, size: Number(e.target.value)})} placeholder="Size" className="gown-detail-edit-input" />
                                <input type="number" value={editForm.price} onChange={e => setEditForm({...editForm, price: Number(e.target.value)})} placeholder="Price" className="gown-detail-edit-input" />
                                <label className="gown-detail-checkbox">
                                    <input type="checkbox" checked={editForm.isActive} onChange={e => setEditForm({...editForm, isActive: e.target.checked})} />
                                    &nbsp;In stock
                                </label>
                            </>
                        ) : (
                            <>
                                <div className="gown-detail-field"><span>Size</span><strong>{detail.size}</strong></div>
                                <div className="gown-detail-field"><span>Price</span><strong>{detail.price}₪</strong></div>
                                <div className="gown-detail-field"><span>Status</span><strong>{!detail.isActive ? 'Out of stock' : detail.isAvailable ? 'Available' : 'Rented on that date'}</strong></div>
                            </>
                        )}
                        <div className="gown-detail-actions">
                            {isEditing ? (
                                <>
                                    <button className="gown-update-submit-btn" onClick={() => handleSave(detail)}>Save</button>
                                    <button className="gown-update-cancel-btn" onClick={() => setEditingId(null)}>Cancel</button>
                                </>
                            ) : (
                                <button className="gown-update-btn" title="Edit size" aria-label="Edit size" onClick={() => handleEditClick(detail)}><FaEdit /></button>
                            )}
                            <button className="gown-delete-btn" title="Delete size" aria-label="Delete size" onClick={() => handleDelete(detail)}><FaTrashAlt /></button>
                            <button
                                className={`gown-cart-btn ${inCart ? 'gown-cart-btn--active' : ''} ${!detail.isAvailable ? 'gown-cart-btn--disabled' : ''}`}
                                title={!detail.isActive ? 'Out of stock' : !detail.isAvailable ? 'Not available on that date' : inCart ? 'Remove from cart' : 'Add to cart'}
                                onClick={() => dispatch(addToCart(toCartItem(detail, gownInfo)))}
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
