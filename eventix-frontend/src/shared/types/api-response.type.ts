export type ApiResponse<T> = {
    code: string;
    message: string;
    isSuccess: boolean;
    data: T;
}