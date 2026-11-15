/** One size of a gown model. */
export interface GownSize {
    gownId: number;
    size: number;
    modelId: number;
    price: number;
    /** The gown is in the inventory (not retired or damaged). */
    isActive: boolean;
    /** The gown can be rented: in the inventory and, when a date is chosen, not rented around it. */
    isAvailable: boolean;
}

/** A gown model with all of its sizes, as the API returns it. */
export interface GownModelProps {
    id: number;
    name: string;
    color: string;
    description?: string | null;
    image?: string | null;
    details: GownSize[];
}

/** A size chosen for an order, together with what is needed to show it. */
export interface CartItem extends GownSize {
    name: string;
    color: string;
    description?: string | null;
    image?: string | null;
}
