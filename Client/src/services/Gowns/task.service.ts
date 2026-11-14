import http from '../http';

const GOWNS_API_URL = '/Gowns';

/** A new gown. Leave modelId out to create a new model; set it to add a size to an existing model. */
export interface NewGownInput {
    modelId?: number;
    name?: string;
    color?: string;
    description?: string;
    imageUrl?: string | null;
    size: number;
    price: number;
}

/** What can be changed on one size of a gown. */
export interface GownSizeChanges {
    size: number;
    price: number;
    isAvailable: boolean;
}

/** What can be changed on a model (shared by all of its sizes). */
export interface GownModelChanges {
    name: string;
    color: string;
    description?: string;
    imageUrl?: string | null;
}

export const fetchAllGowns = async () => {
    const res = await http.get(GOWNS_API_URL);
    return res.data;
};
export const fetchAllGroupsGowns = async () => {
    const res = await http.get(`${GOWNS_API_URL}/groups`);
    return res.data;
};
export const createGownApi = async (input: NewGownInput) => {
    const res = await http.post(GOWNS_API_URL, input);
    return res.data;
};
export const deleteGownApi = async (id: number) => {
    const res = await http.delete(`${GOWNS_API_URL}/${id}`);
    return res.data;
};
export const deleteGownModelApi = async (modelId: number) => {
    await http.delete(`${GOWNS_API_URL}/models/${modelId}`);
};
export const updateGownSizeApi = async (gownId: number, changes: GownSizeChanges) => {
    const res = await http.put(`${GOWNS_API_URL}/${gownId}`, changes);
    return res.data;
};
export const updateGownModelApi = async (modelId: number, changes: GownModelChanges) => {
    const res = await http.put(`${GOWNS_API_URL}/models/${modelId}`, changes);
    return res.data;
};
// Marks a size as not available; still used by the order flow until orders are handled on the server.
export const updateAvailabilityApi = async (gown: { gownId: number; size: number; price: number }) => {
    return updateGownSizeApi(gown.gownId, { size: gown.size, price: gown.price, isAvailable: false });
};

export const fetchAvailableGownsByDate = async (date: string) => {
    const res = await http.get(`${GOWNS_API_URL}/byDate?date=${date}`);
    return res.data;
}
