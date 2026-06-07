import React from 'react';
import './GownModelDetails.css';
import GownModel, { ModelDetails } from '../gownModel/GownModel';
import { useDispatch, useSelector } from 'react-redux';
import { addToCart } from '../../../redux/Gowns/GownSlice';
import { FaShoppingCart } from 'react-icons/fa';
import { GownModelProps } from '../gownModel/GownModel';

interface GownModelDetailsProps {
    details: ModelDetails[];
    gownInfo: { id: number, name: string, color: string, description: string, image?: string };
}

function GownModelDetails({ details, gownInfo }: GownModelDetailsProps) {
    const dispatch = useDispatch();
    const cart = useSelector((state: any) => state.gowns.Cart);

    return (
        <div className="gown-model-details">
            {details.map((detail) => {
                const inCart = cart.some((item: any) => item.details?.[0]?.gownId === detail.gownId);
                return (
                    <div key={detail.gownId} className="gown-detail-card">
                        <div className="gown-detail-field"><span>Gown ID</span><strong>{detail.gownId}</strong></div>
                        <div className="gown-detail-field"><span>Size</span><strong>{detail.size}</strong></div>
                        <div className="gown-detail-field"><span>Price</span><strong>{detail.price}₪</strong></div>
                        <div className="gown-detail-field"><span>Available</span><strong>{detail.isAvailable ? 'Yes' : 'No'}</strong></div>
                        <button
                            className={`gown-cart-btn ${inCart ? 'gown-cart-btn--active' : ''} ${!detail.isAvailable ? 'gown-cart-btn--disabled' : ''}`}
                            title={!detail.isAvailable ? 'Not available' : inCart ? 'Remove from cart' : 'Add to cart'}
                            onClick={() => dispatch(addToCart({ ...detail, ...gownInfo }))}
                            disabled={!detail.isAvailable}
                        >
                            <FaShoppingCart />
                        </button>
                    </div>
                );
            })}
        </div>
    );
}

export default GownModelDetails;
