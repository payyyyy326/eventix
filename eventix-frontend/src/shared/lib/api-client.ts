import axios from "axios";
import { authStorage } from "./auth-storage";

export const apiClient = axios.create({
    baseURL: process.env.NEXT_PUBLIC_API_URL,
});

apiClient.interceptors.request.use((config) => {
    const token = authStorage.getToken();

    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});