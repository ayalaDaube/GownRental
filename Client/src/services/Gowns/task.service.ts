import axios from 'axios';

const GOWNS_API_URL = 'https://localhost:7265/api/Gowns';

export const fetchAllGowns = async () => {
    const res = await axios.get(GOWNS_API_URL);
    return res.data;
};
export const fetchAllGroupsGowns = async () => {
    const res = await axios.get(`${GOWNS_API_URL}/groups`);
    return res.data;
};
export const createGownApi = async (gownData: any) => {
    const payload = {
        id: gownData.details[0].gownId,
        name: gownData.name,
        color: gownData.color,
        description: gownData.description,
        imageUrl: gownData.image || null,
        isAvailable: gownData.details[0].isAvailable,
        modelId: gownData.id,
        price: gownData.details[0].price,
        size: gownData.details[0].size
    };
    const res = await axios.post(GOWNS_API_URL, payload);
    return res.data;
};
export const deleteGownApi = async (id: number) => {
    const res = await axios.delete(`${GOWNS_API_URL}/${id}`);
    return res.data;
};
export const updateGownApi = async (gownData: any) => {
    const res = await axios.put(`${GOWNS_API_URL}`, gownData);
    return res.data;
};
export const updateAvailabilityApi = async (gownData: any) => {
    const payload = {
        id: gownData.gownId,
        name: gownData.name,
        color: gownData.color,
        description: gownData.description,
        imageUrl: gownData.image || null,
        isAvailable: false,
        modelId: gownData.modelId,
        price: gownData.price,
        size: gownData.size
    };
    const res = await axios.put(`${GOWNS_API_URL}`, payload);
    return res.data;
};

export const fetchAvailableGownsByDate = async (date: string) => {
    const res = await axios.get(`${GOWNS_API_URL}/byDate?date=${date}`);
    return res.data;
}

