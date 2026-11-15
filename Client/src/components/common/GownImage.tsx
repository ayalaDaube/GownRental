import React, { useEffect, useState } from 'react';
import './GownImage.css';

const FALLBACKS = ['dress1.svg', 'dress2.svg', 'dress3.svg'];

/** The bundled picture used when a gown has no picture of its own, or its picture cannot be loaded. */
export const fallbackImage = (gownId: number): string =>
  `${process.env.PUBLIC_URL}/assets/${FALLBACKS[Math.abs(gownId) % FALLBACKS.length]}`;

type Props = { gownId: number; name: string; src?: string | null };

function GownImage({ gownId, name, src }: Props) {
    const [failed, setFailed] = useState(false);
    // A new address (after the gown is edited) deserves a new attempt.
    useEffect(() => setFailed(false), [src]);

    const url = src && !failed ? src : fallbackImage(gownId);
    return (
        <img
            className="gown-image"
            src={url}
            alt={name}
            loading="lazy"
            onError={() => setFailed(true)}
        />
    );
}

export default GownImage;
