// import React from 'react';
// import Update from '../../components/gown/update/Update';
// import './Gown.css';
// import DeleteButton from '../../components/gown/deleteButton/DeleteButton';
// import { addToCart } from '../../redux/Gowns/GownSlice';

// export interface GownProps {
//     id: number
//     name: string
//     color: string
//     description: string
//     size: string
//     price: number
//     image: string
//     isAvailable: boolean
//     modelId: number
// }

// function Gown({ id, name, color, description, size, price, image, isAvailable, modelId }: GownProps ) {
//     return (
//         <div className="gown-card">
//             <div className="gown-field"><span>ID</span><strong>{id}</strong></div>
//             <div className="gown-field"><span>Name</span><strong>{name}</strong></div>
//             <div className="gown-field"><span>Color</span><strong>{color}</strong></div>
//             <div className="gown-field"><span>Description</span><strong>{description}</strong></div>
//             <div className="gown-field"><span>Size</span><strong>{size}</strong></div>
//             <div className="gown-field"><span>Price</span><strong>{price}</strong></div>
//             <div className="gown-field"><span>Available</span><strong>{isAvailable ? 'Yes' : 'No'}</strong></div>
//             <div className="gown-field"><span>Model ID</span><strong>{modelId}</strong></div>
//             <div className="gown-actions">
//                 <Update gown={{ id, name, color, description, size, price, image, isAvailable, modelId }} />
//                 <DeleteButton gownId={id} /> 
//                 <button onClick={() => { addToCart({ id, name, color, description, size, price, image, isAvailable, modelId }) }}>Add to Cart</button>   
//             </div>
//         </div>
//     );
// }

// export default Gown;
