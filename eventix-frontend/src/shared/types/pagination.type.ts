export type PaginationRequest<T = unknown> = {
    currentPage: number;
    pageSize: number;
};

export type PaginationResponse<T = unknown> = {
    dataList: T[];
    totalPages: number;
    totalRows: number;
    currentPage: number;
    pageSize: number;
};