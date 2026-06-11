export function getImageUrl(url?: string | null) {
    if (!url) return null;

    if (url.startsWith("http")) {
        return url;
    }

    const baseUrl = process.env.NEXT_PUBLIC_API_URL?.replace("/api", "");

    return `${baseUrl}${url}`;
}